import { openDB, deleteDB } from 'idb';

interface DatabaseInfo {
    databaseName: string;
    storeName: string | undefined | null;
    version: number | undefined | null;
    keyPath: string | undefined | null;
}

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
                    db.createObjectStore(
                        databaseInfo.storeName ?? databaseInfo.databaseName, {
                            keyPath: databaseInfo.keyPath,
                    });
                }
            });
        return database;
    } catch (e) {
        console.error(e);
        return null;
    }
}

export async function addValue(databaseInfo: DatabaseInfo, value: any) {
    const db = await openDatabase(databaseInfo);
    if (!db) {
        return;
    }
    try {
        await db.add(databaseInfo.storeName ?? databaseInfo.databaseName, value);
    } catch (e) {
        console.error(e);
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
        return;
    }
    try {
        return await db.delete(databaseInfo.storeName ?? databaseInfo.databaseName, key);
    } catch (e) {
        console.error(e);
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

export async function putValue(databaseInfo: DatabaseInfo, value: any) {
    const db = await openDatabase(databaseInfo);
    if (!db) {
        return;
    }
    try {
        await db.put(databaseInfo.storeName ?? databaseInfo.databaseName, value);
    } catch (e) {
        console.error(e);
    }
}
