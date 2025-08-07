import { openDB, deleteDB, IDBPCursorWithValue } from 'idb';

interface BatchOptions {
    reset: boolean;
    skip?: number;
    take?: number;
    typeDiscriminator?: string;
    typeDiscriminatorValue?: string;
}

interface CursorInfo {
    db: DatabaseInfo;
    cursor: IDBPCursorWithValue | null;
    key?: IDBValidKey | null;
}

interface DatabaseInfo {
    databaseName: string;
    storeName: string | undefined | null;
    version: number | undefined | null;
    keyPath: string | undefined | null;
    storeNames: string[] | undefined | null;
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

export async function getAll(databaseInfo: DatabaseInfo, asString: boolean = false) {
    const db = await openDatabase(databaseInfo);
    if (!db) {
        return [];
    }
    try {
        const result = await db.getAll(databaseInfo.storeName ?? databaseInfo.databaseName);
        return asString ? result.map(item => JSON.stringify(item)) : result;
    } catch (e) {
        console.error(e);
        return [];
    }
}

export async function getBatch(
    databaseInfo: DatabaseInfo,
    options?: BatchOptions,
    asString: boolean = false) {
    if (options?.take != null && options.take <= 0) {
        return { items: [], more: false };
    }
    const db = await openDatabase(databaseInfo);
    if (!db) {
        return { items: [], more: false };
    }
    const cursorKey = databaseInfo.databaseName + '.' + databaseInfo.storeName;
    if (options?.reset) {
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
            }
        } catch (e) {
            console.error(e);
        }
    }
    if (!cursorInfo || !cursorInfo.cursor) {
        delete cursors[cursorKey];
        return { items: [], more: false };
    }
    const hasTypeDiscriminator = options?.typeDiscriminator != null && options.typeDiscriminatorValue != null;
    if (options?.skip && options.skip > 0 && !hasTypeDiscriminator) {
        cursorInfo.cursor = await cursorInfo.cursor.advance(options.skip);
    }
    if (!cursorInfo || !cursorInfo.cursor) {
        delete cursors[cursorKey];
        return { items: [], more: false };
    }
    const takeAtMost = options?.take != null ? options.take : 20;
    let skipCount = options?.skip && options.skip > 0 ? options.skip : 0;
    const items = [];
    try {
        while (cursorInfo.cursor && items.length < takeAtMost) {
            const current = cursorInfo.cursor.value;
            cursorInfo.cursor = await cursorInfo.cursor.continue();
            if (cursorInfo.cursor) {
                cursorInfo.key = cursorInfo.cursor.key;
            }

            if (hasTypeDiscriminator) {
                if (current[options.typeDiscriminator!] !== options.typeDiscriminatorValue) {
                    const str = current[options.typeDiscriminator!];
                    if (str == null
                        || typeof str !== 'string'
                        || !str.startsWith(options.typeDiscriminatorValue!)) {
                        continue;
                    }
                }
                if (skipCount > 0) {
                    skipCount--;
                    continue;
                }
            }
            if (asString) {
                items.push(JSON.stringify(current));
            } else {
                items.push(current);
            }
        }
    } catch (e) {
        console.error(e);
    }
    let more: boolean;
    if (!cursorInfo || !cursorInfo.cursor) {
        delete cursors[cursorKey];
        more = false;
    } else {
        cursors[cursorKey] = cursorInfo;
        more = true;
    }
    return { items, more };
}

export async function getValue(databaseInfo: DatabaseInfo, key: IDBValidKey, asString: boolean = false) {
    const db = await openDatabase(databaseInfo);
    if (!db) {
        return null;
    }
    try {
        const value = await db.get(databaseInfo.storeName ?? databaseInfo.databaseName, key);
        return asString ? JSON.stringify(value) : value;
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
