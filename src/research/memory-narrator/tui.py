from textual.app import App, ComposeResult
from textual.containers import VerticalScroll
from textual.widgets import Input, Label, Header, Footer, Markdown
from textual import work
import json
import sys
import os
import traceback

# Ensure we can import engine from the same directory
sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from engine import AgenticEngine


class ChatApp(App):
    """Narratoria TUI — consumes the tri-phase AgenticEngine."""

    CSS = """
    Screen {
        layout: vertical;
    }

    #chat-scroll {
        height: 1fr;
        overflow-y: scroll;
        padding: 1;
        background: $surface;
    }

    Input {
        dock: bottom;
        margin: 1;
    }

    .message-user {
        color: $accent;
        text-align: right;
        margin-bottom: 1;
    }

    .message-bot {
        color: $text;
        margin-bottom: 2;
        background: $surface-lighten-1;
        padding: 1;
    }

    .message-meta {
        color: $text-muted;
        margin-bottom: 1;
        text-style: italic;
    }

    .error-message {
        color: red;
        margin-bottom: 2;
        padding: 1;
    }
    """

    # ------------------------------------------------------------------
    # Theme tool (visual hook)
    # ------------------------------------------------------------------

    def apply_bespoke_theme(
        self,
        *,
        surface="#1e1e1e",
        surface_light="#2a2a2a",
        text="#e6e6e6",
        accent="#f7b32b",
        user_align="right",
    ) -> None:
        css = f"""
        Screen {{
            layout: vertical;
        }}

        #chat-scroll {{
            height: 1fr;
            overflow-y: scroll;
            padding: 1;
            background: {surface};
        }}

        Input {{
            dock: bottom;
            margin: 1;
        }}

        .message-user {{
            color: {accent};
            text-align: {user_align};
            margin-bottom: 1;
        }}

        .message-bot {{
            color: {text};
            margin-bottom: 2;
            background: {surface_light};
            padding: 1;
        }}
        """
        self.stylesheet.add_source(css, read_from=("inline", "bespoke_theme"))
        self.refresh_css()

    # ------------------------------------------------------------------
    # Compose / Init
    # ------------------------------------------------------------------

    def compose(self) -> ComposeResult:
        yield Header()
        with VerticalScroll(id="chat-scroll"):
            yield Label(
                "Welcome to Narratoria Agentic Prototype  (Think → Execute → Narrate)",
                classes="message-bot",
            )
        yield Input(placeholder="Type your message...", id="input")
        yield Footer()

    def __init__(self, engine: AgenticEngine, **kwargs):
        super().__init__(**kwargs)
        self.engine = engine
        self._tool_registry: dict[str, callable] = {
            "apply_bespoke_theme": self._tool_apply_bespoke_theme,
        }

    def on_mount(self) -> None:
        scroll = self.query_one("#chat-scroll")
        scroll.mount(
            Label("Engine ready. State deltas will be persisted to ObjectBox.", classes="message-bot")
        )
        
        # Display opening narration if campaign was loaded
        if hasattr(self, "_opening_narration") and self._opening_narration:
            scroll.mount(
                Markdown(self._opening_narration, classes="message-bot")
            )
            scroll.scroll_end(animate=False)

    # ------------------------------------------------------------------
    # Input handling
    # ------------------------------------------------------------------

    async def on_input_submitted(self, event: Input.Submitted) -> None:
        if not self.engine:
            return

        user_text = event.value.strip()
        if not user_text:
            return

        event.input.value = ""

        scroll = self.query_one("#chat-scroll")
        scroll.mount(Label(f"You: {user_text}", classes="message-user"))
        scroll.scroll_end(animate=False)

        # Meta line: shows what phase is running
        meta = Label("\\[thinking...]", classes="message-meta")
        scroll.mount(meta)
        scroll.scroll_end(animate=False)

        # Placeholder for narrated prose
        bot_msg = Markdown("", classes="message-bot")
        scroll.mount(bot_msg)
        scroll.scroll_end(animate=False)

        self.run_turn(user_text, meta, bot_msg)

    # ------------------------------------------------------------------
    # Tri-phase turn (runs in worker thread)
    # ------------------------------------------------------------------

    @work(exclusive=True, thread=True)
    def run_turn(self, user_text: str, meta: Label, widget: Markdown):
        """Execute the full Think → Execute → Narrate loop."""
        try:
            # Phase I — THINK
            self.call_from_thread(meta.update, "\\[Phase I] Thinking...")
            manifest, _deltas = self.engine.think(user_text)

            intent_label = manifest.intent
            tool_count = len(manifest.tool_calls)
            delta_count = len(manifest.state_deltas)
            rag_count = len(manifest.rag_queries)
            meta_text = (
                f"\\[intent={intent_label}  tools={tool_count}  "
                f"deltas={delta_count}  rag_queries={rag_count}]"
            )
            self.call_from_thread(meta.update, meta_text)

            # Phase II — EXECUTE
            exec_ctx = self.engine.execute(
                manifest,
                tool_handler=self._execute_tools,
            )

            # Phase III — NARRATE
            full_response = ""
            for chunk in self.engine.narrate(user_text, manifest, exec_ctx):
                full_response += chunk
                self.call_from_thread(widget.update, full_response)
                self.call_from_thread(
                    self.query_one("#chat-scroll").scroll_end, animate=False
                )

            # If narration produced nothing, show the manifest narrative
            if not full_response.strip():
                self.call_from_thread(widget.update, manifest.narrative)

        except Exception:
            error_msg = traceback.format_exc()
            self.call_from_thread(widget.update, f"Error:\n```\n{error_msg}\n```")

    # ------------------------------------------------------------------
    # Tool execution (Phase II delegate)
    # ------------------------------------------------------------------

    def _execute_tools(self, tools: list[dict]) -> list[dict]:
        """Execute tool dicts via the registry.  Called from engine.execute()."""
        results = []
        for tool in tools:
            if not isinstance(tool, dict):
                continue
            name = tool.get("name")
            args = tool.get("args", {})
            handler = self._tool_registry.get(name)
            if not handler:
                results.append({"name": name, "ok": False, "error": "unknown_tool"})
                continue
            if not isinstance(args, dict):
                results.append({"name": name, "ok": False, "error": "invalid_args"})
                continue
            try:
                output = handler(args)
                results.append({"name": name, "ok": True, "output": output})
            except Exception as exc:
                results.append({"name": name, "ok": False, "error": str(exc)})
        return results

    def _tool_apply_bespoke_theme(self, args: dict) -> dict:
        self.apply_bespoke_theme(
            surface=args.get("surface", "#1e1e1e"),
            surface_light=args.get("surface_light", "#2a2a2a"),
            text=args.get("text", "#e6e6e6"),
            accent=args.get("accent", "#f7b32b"),
            user_align=args.get("user_align", "right"),
        )
        return {"status": "applied"}

    # ------------------------------------------------------------------
    # Lifecycle
    # ------------------------------------------------------------------

    def on_unmount(self):
        if self.engine:
            try:
                self.engine.close()
            except Exception:
                pass


if __name__ == "__main__":
    print("Initializing Narratoria agentic engine (this may take a moment)...")
    try:
        engine = AgenticEngine()
        
        # Load campaign if specified
        campaign_path = "campaigns/wizardrun"
        if os.path.exists(campaign_path):
            print(f"Loading campaign from {campaign_path}...")
            opening_narration = engine.load_campaign(campaign_path)
        else:
            print("No campaign found; starting with empty state.")
            opening_narration = None
        
    except Exception:
        traceback.print_exc()
        sys.exit(1)
    
    app = ChatApp(engine)
    app._opening_narration = opening_narration  # Pass opening to TUI
    app.run()
