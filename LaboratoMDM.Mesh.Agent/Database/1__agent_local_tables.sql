-- Политики
CREATE TABLE Policies (
    Hash TEXT PRIMARY KEY,
    Name TEXT NOT NULL,
    Scope TEXT NOT NULL,
    RegistryKey TEXT NOT NULL,
    ValueName TEXT NOT NULL,
    EnabledValue INTEGER,
    DisabledValue INTEGER,
    SourceRevision INTEGER NOT NULL -- ревизия мастера
);

-- Элементы политики
CREATE TABLE PolicyElements (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PolicyHash TEXT NOT NULL,
    ElementId TEXT NOT NULL,
    Type TEXT NOT NULL,
    ValueName TEXT,
    MaxLength INTEGER,
    Required INTEGER,
    ClientExtension TEXT,
    FOREIGN KEY (PolicyHash) REFERENCES Policies(Hash) ON DELETE CASCADE
);

-- Таблица compliance
CREATE TABLE AgentPolicyCompliance (
    PolicyHash TEXT NOT NULL,
    UserSid TEXT,
    State TEXT NOT NULL,
    ActualValue TEXT,
    LastCheckedAt DATETIME NOT NULL,
    PRIMARY KEY (PolicyHash, UserSid)
);
