CREATE TABLE PolicyRevision (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    RevisionNumber INTEGER NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE AgentPolicyCompliance (
    AgentId TEXT NOT NULL,
    PolicyHash TEXT NOT NULL,
    UserSid TEXT,

    State TEXT NOT NULL,
    ActualValue TEXT,
    LastCheckedAt DATETIME NOT NULL,

    PRIMARY KEY (AgentId, PolicyHash, UserSid)
);