"""
Tri-Phase Agentic Engine for Narratoria.

Architecture: THINK -> EXECUTE -> NARRATE
- Phase I  (THINK):   Gemma produces a CognitiveManifest (JSON) — intent,
                       RAG queries, tool calls, state deltas, narration directive.
- Phase II (EXECUTE):  Python validates the manifest, runs RAG queries,
                       executes tools, and applies state deltas.
- Phase III (NARRATE): Gemma generates constrained prose from validated context.

Storage:
- StateDelta:   Deterministic record of what changed each turn.
- EmbeddedFact: Vector-indexed knowledge for semantic retrieval.

No raw chat is persisted.  Context is reconstructed every turn from
the last N deltas + RAG results.
"""

import json
import os
import time
from dataclasses import dataclass, field, asdict
from enum import Enum
from typing import Generator

# ---------------------------------------------------------------------------
# macOS / Python 3.14 multiprocessing guards — set BEFORE torch/transformers
# ---------------------------------------------------------------------------
os.environ["TOKENIZERS_PARALLELISM"] = "false"
os.environ["OMP_NUM_THREADS"] = "1"
os.environ["MKL_NUM_THREADS"] = "1"
os.environ["OPENBLAS_NUM_THREADS"] = "1"

import objectbox
from sentence_transformers import SentenceTransformer
from llama_cpp import Llama

# ===================================================================
# Configuration
# ===================================================================

GEMMA_REPO = "bartowski/google_gemma-3n-E2B-it-GGUF"
GEMMA_FILE = "google_gemma-3n-E2B-it-Q2_K.gguf"
EMBEDDING_MODEL = "sentence-transformers/all-MiniLM-L6-v2"
DB_DIR = "memory_prototype_db"

# Retrieval knobs
DELTA_HISTORY_LIMIT = 5
RAG_QUERY_LIMIT = 3        # max queries the Thinker may request
RAG_RESULTS_PER_QUERY = 3
SEMANTIC_MAX_DISTANCE = 0.35

# Retry bounds (Constitution IV.A)
MAX_THINK_RETRIES = 3

# ===================================================================
# Intent enum — the Thinker must pick one of these
# ===================================================================

class Intent(str, Enum):
    THEME_CHANGE = "theme_change"
    MEMORY_RECALL = "memory_recall"
    COMBAT = "combat"
    DIALOGUE = "dialogue"
    EXPLORATION = "exploration"
    INVENTORY = "inventory"
    SMALLTALK = "smalltalk"

VALID_INTENTS = {e.value for e in Intent}

# ===================================================================
# Tool Catalog
# ===================================================================

TOOL_CATALOG: dict[str, dict] = {
    "apply_bespoke_theme": {
        "description": "Apply a runtime UI theme (colors + alignment).",
        "args": {
            "surface": "#1e1e1e",
            "surface_light": "#2a2a2a",
            "text": "#e6e6e6",
            "accent": "#f7b32b",
            "user_align": "right",
        },
    },
}

# ===================================================================
# Cognitive Manifest (Phase I output)
# ===================================================================

@dataclass
class NarrationDirective:
    tone: str = "neutral"
    style: str = "second_person"
    sentences: int = 3
    must_reference: list[str] = field(default_factory=list)
    must_avoid: list[str] = field(default_factory=list)

@dataclass
class ToolCall:
    name: str
    args: dict = field(default_factory=dict)

@dataclass
class DeltaPrediction:
    entity_type: str   # "inventory", "npc_sentiment", "scene", ...
    entity_id: str     # "dagger", "marta", "tavern_entrance"
    action: str        # "add", "remove", "update"
    value: str = ""    # JSON-encoded value or simple string

VALID_DELTA_ACTIONS = {"add", "remove", "update"}

@dataclass
class CognitiveManifest:
    intent: str
    narrative: str
    rag_queries: list[str] = field(default_factory=list)
    tool_calls: list[ToolCall] = field(default_factory=list)
    state_deltas: list[DeltaPrediction] = field(default_factory=list)
    narration_directive: NarrationDirective = field(default_factory=NarrationDirective)

# ===================================================================
# Database Entities
# ===================================================================

@objectbox.Entity()
class StateDelta:
    id = objectbox.Id
    turn_number = objectbox.Int64
    timestamp = objectbox.Float64
    entity_type = objectbox.String
    entity_id = objectbox.String
    action = objectbox.String
    value = objectbox.String

@objectbox.Entity()
class EmbeddedFact:
    id = objectbox.Id
    turn_number = objectbox.Int64
    timestamp = objectbox.Float64
    content = objectbox.String
    source = objectbox.String
    embedding = objectbox.Float32Vector(
        index=objectbox.HnswIndex(
            dimensions=384,
            distance_type=objectbox.VectorDistanceType.COSINE,
        )
    )

# ===================================================================
# Engine
# ===================================================================

class AgenticEngine:
    """Tri-phase agentic loop: Think -> Execute -> Narrate."""

    def __init__(self) -> None:
        self._turn: int = 0

        # 1. Embedder
        print("[engine] Step 1/3: Loading embedder...")
        self.embedder = SentenceTransformer(EMBEDDING_MODEL, device="cpu")

        # 2. ObjectBox
        print(f"[engine] Step 2/3: Opening database at {DB_DIR}...")
        model = objectbox.Model()
        model.entity(StateDelta)
        model.entity(EmbeddedFact)
        self.store = objectbox.Store(model=model, directory=DB_DIR)
        self.delta_box = self.store.box(StateDelta)
        self.fact_box = self.store.box(EmbeddedFact)

        # Recover turn counter from persisted deltas
        all_deltas = self.delta_box.query().build().find()
        if all_deltas:
            self._turn = max(d.turn_number for d in all_deltas)

        # 3. LLM (single model, two call modes)
        print(f"[engine] Step 3/3: Loading LLM ({GEMMA_FILE})...")
        self.llm = Llama.from_pretrained(
            repo_id=GEMMA_REPO,
            filename=GEMMA_FILE,
            n_ctx=4096,
            n_threads=1,
            verbose=False,
        )
        print("[engine] Engine ready.")

    # ------------------------------------------------------------------
    # Storage helpers
    # ------------------------------------------------------------------

    def get_recent_deltas(self, limit: int = DELTA_HISTORY_LIMIT) -> list[dict]:
        """Return the most recent state deltas (oldest -> newest)."""
        results = self.delta_box.query().build().find()
        results.sort(key=lambda d: d.timestamp, reverse=True)
        recent = list(reversed(results[:limit]))
        return [
            {
                "turn": d.turn_number,
                "entity_type": d.entity_type,
                "entity_id": d.entity_id,
                "action": d.action,
                "value": d.value,
            }
            for d in recent
        ]

    def semantic_search(self, query: str, limit: int = RAG_RESULTS_PER_QUERY) -> list[str]:
        """Find relevant EmbeddedFacts by vector similarity."""
        if not query:
            return []
        vec = self.embedder.encode(query).tolist()
        qb = self.fact_box.query(
            EmbeddedFact.embedding.nearest_neighbor(vec, limit)
        )
        results = qb.build().find_with_scores()
        lines = []
        for fact, score in results:
            if score > SEMANTIC_MAX_DISTANCE:
                continue
            lines.append(fact.content)
        return lines

    def persist_deltas(self, deltas: list[DeltaPrediction], turn: int) -> None:
        """Write validated deltas into ObjectBox."""
        now = time.time()
        for d in deltas:
            row = StateDelta()
            row.turn_number = turn
            row.timestamp = now
            row.entity_type = d.entity_type
            row.entity_id = d.entity_id
            row.action = d.action
            row.value = d.value
            self.delta_box.put(row)

    def persist_fact(self, content: str, source: str, turn: int) -> None:
        """Embed and store a fact for future RAG retrieval."""
        if not content:
            return
        vec = self.embedder.encode(content).tolist()
        row = EmbeddedFact()
        row.turn_number = turn
        row.timestamp = time.time()
        row.content = content
        row.source = source
        row.embedding = vec
        self.fact_box.put(row)

    # ------------------------------------------------------------------
    # Phase I — THINK
    # ------------------------------------------------------------------

    def _think_prompt(self, user_input: str, recent_deltas: list[dict]) -> str:
        tool_lines = []
        for name, info in TOOL_CATALOG.items():
            args_json = json.dumps(info["args"], ensure_ascii=True)
            tool_lines.append(f"  - {name}: {info['description']} args={args_json}")
        tools_block = "\n".join(tool_lines) if tool_lines else "  (none)"

        deltas_json = json.dumps(recent_deltas, ensure_ascii=True)
        intents_str = ", ".join(sorted(VALID_INTENTS))

        return (
            "You are the Thinking Layer for a narrative RPG engine.\n"
            "Return ONLY a single JSON object. No markdown, no explanation.\n\n"
            "JSON schema:\n"
            "{\n"
            '  "intent": "<one of: ' + intents_str + '>",\n'
            '  "narrative": "<1-sentence summary of what happens this turn>",\n'
            '  "rag_queries": ["<search query>", ...],  // max 3, or empty\n'
            '  "tool_calls": [{"name": "<tool>", "args": {...}}, ...],  // or empty\n'
            '  "state_deltas": [{"entity_type": "...", "entity_id": "...", "action": "add|remove|update", "value": "..."}, ...],\n'
            '  "narration_directive": {"tone": "...", "style": "second_person", "sentences": 3, "must_reference": [], "must_avoid": []}\n'
            "}\n\n"
            "Rules:\n"
            "- intent MUST be one of: " + intents_str + "\n"
            "- tool_calls MUST only use tools from the catalog below\n"
            "- state_deltas predict what changes this turn; engine validates\n"
            "- narration_directive tells the narrator how to write the response\n"
            "- If nothing changes, use empty arrays and intent=smalltalk\n\n"
            "Available tools:\n"
            f"{tools_block}\n\n"
            "Recent state deltas:\n"
            f"{deltas_json}\n\n"
            f"Player says: {user_input}"
        )

    def think(self, user_input: str) -> tuple[CognitiveManifest, list[dict]]:
        """Phase I: generate and validate a CognitiveManifest."""
        recent_deltas = self.get_recent_deltas()
        prompt = self._think_prompt(user_input, recent_deltas)

        for attempt in range(1, MAX_THINK_RETRIES + 1):
            result = self.llm.create_chat_completion(
                messages=[{"role": "user", "content": prompt}],
                max_tokens=256,
                stream=False,
                temperature=0.2,
            )
            raw = result["choices"][0]["message"]["content"].strip()
            manifest = self._parse_manifest(raw)
            if manifest is not None:
                return manifest, recent_deltas

            print(f"[engine] THINK attempt {attempt}/{MAX_THINK_RETRIES} failed to parse")

        # Exhausted retries -> fallback manifest
        return self._fallback_manifest(user_input), recent_deltas

    def _parse_manifest(self, raw: str) -> CognitiveManifest | None:
        """Parse raw LLM output into a validated CognitiveManifest."""
        data = self._extract_json(raw)
        if data is None or not isinstance(data, dict):
            return None

        # --- intent ---
        intent = data.get("intent", "")
        if intent not in VALID_INTENTS:
            return None

        # --- narrative ---
        narrative = data.get("narrative", "")
        if not isinstance(narrative, str) or not narrative:
            return None

        # --- rag_queries ---
        rag_queries = data.get("rag_queries", [])
        if not isinstance(rag_queries, list):
            rag_queries = []
        rag_queries = [q for q in rag_queries if isinstance(q, str)][:RAG_QUERY_LIMIT]

        # --- tool_calls (strip unknown tools) ---
        raw_tools = data.get("tool_calls", [])
        if not isinstance(raw_tools, list):
            raw_tools = []
        tool_calls: list[ToolCall] = []
        for t in raw_tools:
            if not isinstance(t, dict):
                continue
            name = t.get("name", "")
            if name not in TOOL_CATALOG:
                continue
            args = t.get("args", {})
            if not isinstance(args, dict):
                args = {}
            tool_calls.append(ToolCall(name=name, args=args))

        # --- state_deltas (strip invalid) ---
        raw_deltas = data.get("state_deltas", [])
        if not isinstance(raw_deltas, list):
            raw_deltas = []
        state_deltas: list[DeltaPrediction] = []
        for d in raw_deltas:
            if not isinstance(d, dict):
                continue
            action = d.get("action", "")
            if action not in VALID_DELTA_ACTIONS:
                continue
            state_deltas.append(DeltaPrediction(
                entity_type=str(d.get("entity_type", "")),
                entity_id=str(d.get("entity_id", "")),
                action=action,
                value=str(d.get("value", "")),
            ))

        # --- narration_directive ---
        raw_dir = data.get("narration_directive", {})
        if not isinstance(raw_dir, dict):
            raw_dir = {}
        directive = NarrationDirective(
            tone=str(raw_dir.get("tone", "neutral")),
            style=str(raw_dir.get("style", "second_person")),
            sentences=min(int(raw_dir.get("sentences", 3)), 5),
            must_reference=[str(r) for r in raw_dir.get("must_reference", []) if isinstance(r, str)],
            must_avoid=[str(a) for a in raw_dir.get("must_avoid", []) if isinstance(a, str)],
        )

        return CognitiveManifest(
            intent=intent,
            narrative=narrative,
            rag_queries=rag_queries,
            tool_calls=tool_calls,
            state_deltas=state_deltas,
            narration_directive=directive,
        )

    def _fallback_manifest(self, user_input: str) -> CognitiveManifest:
        return CognitiveManifest(
            intent=Intent.SMALLTALK.value,
            narrative=(
                "I may have missed your intent. "
                "Please rephrase or be more specific. "
                f"Your message was: {user_input}"
            ),
            narration_directive=NarrationDirective(tone="apologetic", sentences=2),
        )

    # ------------------------------------------------------------------
    # Phase II — EXECUTE
    # ------------------------------------------------------------------

    def execute(
        self,
        manifest: CognitiveManifest,
        tool_handler=None,
    ) -> dict:
        """
        Phase II: deterministic execution.

        1. Run RAG queries from the manifest.
        2. Execute tool calls (delegated to tool_handler from TUI).
        3. Validate and persist state deltas.

        Returns an execution_context dict consumed by Phase III.
        """
        self._turn += 1
        turn = self._turn

        # 1. RAG retrieval
        rag_results: list[str] = []
        for query in manifest.rag_queries:
            rag_results.extend(self.semantic_search(query))
        # Deduplicate while preserving order
        seen: set[str] = set()
        unique_rag: list[str] = []
        for r in rag_results:
            if r not in seen:
                seen.add(r)
                unique_rag.append(r)

        # 2. Tool execution
        tool_results: list[dict] = []
        if tool_handler and manifest.tool_calls:
            tool_dicts = [asdict(tc) for tc in manifest.tool_calls]
            tool_results = tool_handler(tool_dicts)

        # 3. Persist deltas
        if manifest.state_deltas:
            self.persist_deltas(manifest.state_deltas, turn)

        # 4. Persist a turn-summary fact for future RAG
        summary = manifest.narrative
        if summary:
            self.persist_fact(summary, source="turn_summary", turn=turn)

        return {
            "turn": turn,
            "rag_results": unique_rag,
            "tool_results": tool_results,
        }

    # ------------------------------------------------------------------
    # Phase III — NARRATE
    # ------------------------------------------------------------------

    def _narrate_prompt(
        self,
        user_input: str,
        manifest: CognitiveManifest,
        exec_ctx: dict,
    ) -> str:
        directive = manifest.narration_directive

        facts_block = "\n".join(f"- {f}" for f in exec_ctx["rag_results"]) or "(none)"
        tools_block = json.dumps(exec_ctx["tool_results"], ensure_ascii=True) if exec_ctx["tool_results"] else "(none)"
        deltas_block = json.dumps(
            [asdict(d) for d in manifest.state_deltas], ensure_ascii=True
        ) if manifest.state_deltas else "(none)"

        ref_str = ", ".join(directive.must_reference) if directive.must_reference else "(none)"
        avoid_str = ", ".join(directive.must_avoid) if directive.must_avoid else "(none)"

        # Map style token to an explicit POV instruction
        pov_instructions = {
            "second_person": (
                'Write in second person: use "you" and "your" exclusively. '
                'NEVER use first person ("I", "my", "me") or third person ("he", "she", "they").'
            ),
            "first_person": (
                'Write in first person: use "I" and "my". '
                'NEVER use second person ("you", "your").'
            ),
            "third_person": (
                'Write in third person: use the character\'s name or "they". '
                'NEVER use first person ("I") or second person ("you").'
            ),
        }
        pov_rule = pov_instructions.get(
            directive.style,
            pov_instructions["second_person"],
        )

        return (
            f"You are a narrator for a narrative RPG.\n"
            f"Tone: {directive.tone}.\n"
            f"{pov_rule}\n"
            f"Write exactly {directive.sentences} sentence(s).\n"
            f"You MUST reference: {ref_str}\n"
            f"You MUST NOT mention: {avoid_str}\n\n"
            f"Relevant facts:\n{facts_block}\n\n"
            f"State changes this turn:\n{deltas_block}\n\n"
            f"Tool results:\n{tools_block}\n\n"
            f"Turn summary: {manifest.narrative}\n\n"
            f"Player said: {user_input}\n\n"
            f"Narrate:"
        )

    def narrate(
        self,
        user_input: str,
        manifest: CognitiveManifest,
        exec_ctx: dict,
    ) -> Generator[str, None, None]:
        """Phase III: stream constrained prose to the TUI."""
        prompt = self._narrate_prompt(user_input, manifest, exec_ctx)

        stream = self.llm.create_chat_completion(
            messages=[{"role": "user", "content": prompt}],
            max_tokens=512,
            stream=True,
            temperature=0.7,
        )

        for chunk in stream:
            delta = chunk["choices"][0]["delta"].get("content", "")
            if delta:
                yield delta

    # ------------------------------------------------------------------
    # JSON extraction (reused from prior prototype)
    # ------------------------------------------------------------------

    @staticmethod
    def _extract_json(raw: str) -> dict | None:
        """Best-effort JSON extraction from potentially messy LLM output."""
        if not raw:
            return None

        cleaned = raw.strip()

        try:
            return json.loads(cleaned)
        except json.JSONDecodeError:
            pass

        # Fenced code block
        if "```" in cleaned:
            start = cleaned.find("```")
            end = cleaned.rfind("```")
            if start != end:
                fenced = cleaned[start + 3 : end].strip()
                if fenced.lower().startswith("json"):
                    fenced = fenced[4:].strip()
                try:
                    return json.loads(fenced)
                except json.JSONDecodeError:
                    pass

        # raw_decode
        try:
            parsed, _ = json.JSONDecoder().raw_decode(cleaned)
            return parsed
        except json.JSONDecodeError:
            pass

        # Brace extraction
        brace_start = cleaned.find("{")
        brace_end = cleaned.rfind("}")
        if brace_start != -1 and brace_end > brace_start:
            try:
                return json.loads(cleaned[brace_start : brace_end + 1])
            except json.JSONDecodeError:
                return None

        return None

    # ------------------------------------------------------------------
    # Lifecycle
    # ------------------------------------------------------------------

    def close(self) -> None:
        self.store.close()
