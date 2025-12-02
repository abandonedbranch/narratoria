const databaseCache = new Map();

function applySchemaUpgrade(db, transaction, schema) {
  if (!transaction) {
    throw new Error("Upgrade transaction missing");
  }

  const stores = schema.Stores ?? [];
  for (const storeDef of stores) {
    const hasStore = db.objectStoreNames.contains(storeDef.Name);
    const store = hasStore
      ? transaction.objectStore(storeDef.Name)
      : db.createObjectStore(storeDef.Name, {
          keyPath: storeDef.KeyPath,
          autoIncrement: Boolean(storeDef.AutoIncrement),
        });

    const indexes = storeDef.Indexes ?? [];
    for (const indexDef of indexes) {
      if (!store.indexNames.contains(indexDef.Name)) {
        store.createIndex(indexDef.Name, indexDef.KeyPath, {
          unique: Boolean(indexDef.Unique),
          multiEntry: Boolean(indexDef.MultiEntry),
        });
      }
    }
  }
}

function openDatabase(schema) {
  if (typeof indexedDB === "undefined") {
    throw new Error("IndexedDB not supported");
  }

  const cached = databaseCache.get(schema.DatabaseName);
  if (cached && cached.version === schema.Version) {
    return Promise.resolve(cached.db);
  }

  if (cached?.db) {
    cached.db.close();
    databaseCache.delete(schema.DatabaseName);
  }

  return new Promise((resolve, reject) => {
    const request = indexedDB.open(schema.DatabaseName, schema.Version);
    request.onupgradeneeded = () => {
      applySchemaUpgrade(request.result, request.transaction, schema);
    };
    request.onerror = () => reject(request.error ?? new Error("IndexedDB open failed"));
    request.onblocked = () => reject(new Error("IndexedDB upgrade blocked"));
    request.onsuccess = () => {
      const db = request.result;
      db.onversionchange = () => {
        db.close();
        databaseCache.delete(schema.DatabaseName);
      };
      databaseCache.set(schema.DatabaseName, { db, version: schema.Version });
      resolve(db);
    };
  });
}

function buildRecord(keyPath, key, payload, indexValues) {
  const record = { Payload: payload };
  if (keyPath) {
    record[keyPath] = key;
  }

  if (indexValues) {
    for (const [k, v] of Object.entries(indexValues)) {
      record[k] = v;
    }
  }

  return record;
}

export async function put(args) {
  const db = await openDatabase(args.Schema);
  const storeName = args.StoreName;
  const keyPath = args.KeyPath;

  return new Promise((resolve, reject) => {
    const transaction = db.transaction([storeName], "readwrite");
    const store = transaction.objectStore(storeName);
    const record = buildRecord(keyPath, args.Key, args.Payload, args.IndexValues);
    const request = store.put(record);

    request.onerror = () => reject(request.error ?? new Error("Put failed"));
    transaction.onabort = () => reject(transaction.error ?? new Error("Transaction aborted"));
    transaction.onerror = () => reject(transaction.error ?? new Error("Transaction failed"));
    transaction.oncomplete = () => resolve(true);
  });
}

export async function list(args) {
  const db = await openDatabase(args.Schema);
  const results = [];
  const storeName = args.StoreName;
  const keyPath = args.KeyPath;
  const query = args.Query;

  await new Promise((resolve, reject) => {
    const transaction = db.transaction([storeName], "readonly");
    const store = transaction.objectStore(storeName);
    const source = query?.IndexName ? store.index(query.IndexName) : store;
    let request;

    if (query?.IndexName && query?.MatchValue !== undefined && query?.MatchValue !== null) {
      request = source.getAll(IDBKeyRange.only(query.MatchValue));
    } else {
      request = source.getAll();
    }

    request.onerror = () => reject(request.error ?? new Error("List failed"));
    request.onsuccess = () => {
      const records = request.result ?? [];
      for (const record of records) {
        results.push({ Key: record[keyPath], Payload: record.Payload ?? new Uint8Array() });
        if (query?.Limit && results.length >= query.Limit) {
          break;
        }
      }
    };

    transaction.onabort = () => reject(transaction.error ?? new Error("Transaction aborted"));
    transaction.onerror = () => reject(transaction.error ?? new Error("Transaction failed"));
    transaction.oncomplete = () => resolve(true);
  });

  return results;
}
