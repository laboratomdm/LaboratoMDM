CREATE TABLE AgentPolicyCompliance (
    AgentId TEXT NOT NULL,
    PolicyHash TEXT NOT NULL,
    UserSid TEXT,

    State TEXT NOT NULL,
    ActualValue TEXT,
    LastCheckedAt DATETIME NOT NULL,

    PRIMARY KEY (AgentId, PolicyHash, UserSid)
);