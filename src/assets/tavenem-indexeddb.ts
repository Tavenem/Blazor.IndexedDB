import { openDB, deleteDB, IDBPCursorWithValue } from 'idb';

interface DatabaseInfo {
    databaseName: string;
    storeName: string | undefined | null;
    version: number | undefined | null;
    keyPath: string | undefined | null;
    storeNames: string[] | undefined | null;
}

interface CursorInfo {
    db: DatabaseInfo;
    cursor: IDBPCursorWithValue | null;
    key?: IDBValidKey | null;
}

const cursors: Record<string, CursorInfo> = {};

async function openDatabase(databaseInfo: DatabaseInfo) {
    if (databaseInfo.version === null) {
        databaseInfo.version = undefined;
    }
    databaseInfo.keyPath ??= 'id';

    try {
        const database = await openDB(
            databaseInfo.databaseName,
            databaseInfo.version, {
                upgrade(db) {
                    if (databaseInfo.storeNames) {
                        for (const storeName of databaseInfo.storeNames) {
                            if (!db.objectStoreNames.contains(storeName)) {
                                db.createObjectStore(storeName, {
                                    keyPath: databaseInfo.keyPath,
                                });
                            }
                        }
                    }
                    if (db.objectStoreNames.length == 0
                        && !db.objectStoreNames.contains(databaseInfo.databaseName)) {
                        db.createObjectStore(databaseInfo.databaseName, {
                            keyPath: databaseInfo.keyPath,
                        });
                    }
                }
            });
        return database;
    } catch (e) {
        console.error(e);
        return null;
    }
}

export async function clear(databaseInfo: DatabaseInfo) {
    const db = await openDatabase(databaseInfo);
    if (!db) {
        return;
    }
    try {
        return await db.clear(databaseInfo.storeName ?? databaseInfo.databaseName);
    } catch (e) {
        console.error(e);
    }
}

export async function count(databaseInfo: DatabaseInfo) {
    const db = await openDatabase(databaseInfo);
    if (!db) {
        return 0;
    }
    try {
        return await db.count(databaseInfo.storeName ?? databaseInfo.databaseName);
    } catch (e) {
        console.error(e);
        return 0;
    }
}

export async function deleteDatabase(name: string) {
    try {
        await deleteDB(name);
    } catch (e) {
        console.error(e);
    }
}

export async function deleteValue(databaseInfo: DatabaseInfo, key: IDBValidKey) {
    const db = await openDatabase(databaseInfo);
    if (!db) {
        return false;
    }
    try {
        await db.delete(databaseInfo.storeName ?? databaseInfo.databaseName, key);
        return true;
    } catch (e) {
        console.error(e);
        return false;
    }
}

export async function getAll(databaseInfo: DatabaseInfo) {
    const db = await openDatabase(databaseInfo);
    if (!db) {
        return [];
    }
    try {
        return await db.getAll(databaseInfo.storeName ?? databaseInfo.databaseName);
    } catch (e) {
        console.error(e);
        return [];
    }
}

export async function getAllStrings(databaseInfo: DatabaseInfo) {
    const db = await openDatabase(databaseInfo);
    if (!db) {
        return [];
    }
    try {
        var items = await db.getAll(databaseInfo.storeName ?? databaseInfo.databaseName);
        return items.map(v => JSON.stringify(v));
    } catch (e) {
        console.error(e);
        return [];
    }
}

export async function getBatch(databaseInfo: DatabaseInfo, reset: boolean) {
    const db = await openDatabase(databaseInfo);
    if (!db) {
        return [];
    }
    const cursorKey = databaseInfo.databaseName + '.' + databaseInfo.storeName;
    if (reset) {
        delete cursors[cursorKey];
    }
    let cursorInfo = cursors[cursorKey];
    if (!cursorInfo || cursorInfo.db.version != databaseInfo.version) {
        try {
            const cursor = await db.transaction(databaseInfo.storeName ?? databaseInfo.databaseName).store.openCursor();
            cursorInfo = {
                db: databaseInfo,
                cursor,
            };
        } catch (e) {
            console.error(e);
        }
    } else {
        try {
            cursorInfo.cursor = await db.transaction(databaseInfo.storeName ?? databaseInfo.databaseName).store.openCursor();
            if (cursorInfo.cursor && cursorInfo.key) {
                cursorInfo.cursor = await cursorInfo.cursor.continue(cursorInfo.key);
                if (cursorInfo.cursor) {
                    cursorInfo.cursor = await cursorInfo.cursor.continue();
                    if (cursorInfo.cursor) {
                        cursorInfo.key = cursorInfo.cursor.key;
                    } else {
                        delete cursors[cursorKey];
                        return [];
                    }
                }
            }
        } catch (e) {
            console.error(e);
        }
    }
    if (!cursorInfo) {
        return [];
    }
    const items = [];
    try {
        while (cursorInfo.cursor && items.length < 20) {
            items.push(cursorInfo.cursor.value);
            cursorInfo.cursor = await cursorInfo.cursor.continue();
            cursorInfo.key = cursorInfo.cursor?.key;
        }
    } catch (e) {
        console.error(e);
    }
    cursors[cursorKey] = cursorInfo;
    return items;
}

export async function getBatchStrings(databaseInfo: DatabaseInfo, reset: boolean) {
    const db = await openDatabase(databaseInfo);
    if (!db) {
        return [];
    }
    const cursorKey = databaseInfo.databaseName + '.' + databaseInfo.storeName;
    if (reset) {
        delete cursors[cursorKey];
    }
    let cursorInfo = cursors[cursorKey];
    if (!cursorInfo || cursorInfo.db.version != databaseInfo.version) {
        try {
            const cursor = await db.transaction(databaseInfo.storeName ?? databaseInfo.databaseName).store.openCursor();
            cursorInfo = {
                db: databaseInfo,
                cursor,
            };
        } catch (e) {
            console.error(e);
        }
    } else {
        try {
            cursorInfo.cursor = await db.transaction(databaseInfo.storeName ?? databaseInfo.databaseName).store.openCursor();
            if (cursorInfo.cursor && cursorInfo.key) {
                cursorInfo.cursor = await cursorInfo.cursor.continue(cursorInfo.key);
                if (cursorInfo.cursor) {
                    cursorInfo.cursor = await cursorInfo.cursor.continue();
                    if (cursorInfo.cursor) {
                        cursorInfo.key = cursorInfo.cursor.key;
                    } else {
                        delete cursors[cursorKey];
                        return [];
                    }
                }
            }
        } catch (e) {
            console.error(e);
        }
    }
    if (!cursorInfo) {
        return [];
    }
    const items = [];
    try {
        while (cursorInfo.cursor && items.length < 20) {
            items.push(JSON.stringify(cursorInfo.cursor.value));
            cursorInfo.cursor = await cursorInfo.cursor.continue();
            cursorInfo.key = cursorInfo.cursor?.key;
        }
    } catch (e) {
        console.error(e);
    }
    cursors[cursorKey] = cursorInfo;
    return items;
}

export async function getValue(databaseInfo: DatabaseInfo, key: IDBValidKey) {
    const db = await openDatabase(databaseInfo);
    if (!db) {
        return null;
    }
    try {
        return await db.get(databaseInfo.storeName ?? databaseInfo.databaseName, key);
    } catch (e) {
        console.error(e);
        return null;
    }
}

export async function getValueString(databaseInfo: DatabaseInfo, key: IDBValidKey) {
    const db = await openDatabase(databaseInfo);
    if (!db) {
        return null;
    }
    try {
        return JSON.stringify(await db.get(databaseInfo.storeName ?? databaseInfo.databaseName, key));
    } catch (e) {
        console.error(e);
        return null;
    }
}

export async function putValue(databaseInfo: DatabaseInfo, value: string) {
    const db = await openDatabase(databaseInfo);
    if (!db) {
        return false;
    }
    try {
        await db.put(databaseInfo.storeName ?? databaseInfo.databaseName, JSON.parse(value));
        return true;
    } catch (e) {
        console.error(e);
        return false;
    }
}
