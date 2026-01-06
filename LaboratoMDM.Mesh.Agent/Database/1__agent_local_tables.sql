-- Политики
CREATE TABLE Policies (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    DisplayName TEXT,
    ExplainText TEXT,
    Scope TEXT NOT NULL DEFAULT 'None', -- User, Machine, Both, None
    RegistryKey TEXT NOT NULL DEFAULT '',
    ValueName TEXT NOT NULL DEFAULT '',
    EnabledValue INTEGER,
    DisabledValue INTEGER,
    SupportedOnRef TEXT,
    ParentCategoryRef TEXT,
    PresentationRef TEXT,
    ClientExtension TEXT,
    Hash TEXT NOT NULL, -- хэш политики (для идентичности)
    CONSTRAINT chk_scope CHECK(Scope IN ('None', 'User', 'Machine', 'Both')),
    UNIQUE(Name, Hash)
);

CREATE INDEX idx_policies_name ON Policies(Name);
CREATE INDEX idx_policies_scope ON Policies(Scope);
CREATE INDEX idx_policies_supportedon ON Policies(SupportedOnRef);
CREATE INDEX idx_policies_hash ON Policies(Hash);
CREATE INDEX idx_policies_parentcategory ON Policies(ParentCategoryRef);

-- Элементы политики
CREATE TABLE PolicyElements (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PolicyId INTEGER,
    ElementId TEXT NOT NULL,
    Type TEXT NOT NULL,
    ValueName TEXT,
    MaxLength INTEGER,
    Required INTEGER,
    ClientExtension TEXT, ValuePrefix TEXT,
    ExplicitValue BOOLEAN,
    Additive BOOLEAN,
    MinValue BIGINT,
    MaxValue BIGINT,
    StoreAsText BOOLEAN,
    Expandable BOOLEAN,
    MaxStrings INTEGER,
    FOREIGN KEY (PolicyId) REFERENCES Policies(Id) ON DELETE CASCADE,
    UNIQUE (PolicyId, ElementId)
);

CREATE INDEX idx_elements_policyid ON PolicyElements(PolicyId);
CREATE INDEX idx_elements_type ON PolicyElements(Type);
CREATE INDEX idx_elements_valuename ON PolicyElements(ValueName);

--Элемент элемента политики
CREATE TABLE PolicyElementItems (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    PolicyElementId INTEGER NOT NULL,
    ParentType TEXT NOT NULL CHECK (ParentType IN ('elements', 'enabled_list', 'disabled_list')),
    Type TEXT NOT NULL CHECK (Type IN ('value', 'value_list')),
    ValueType TEXT CHECK (ValueType IN ('decimal', 'string', 'delete')),
    RegistryKey TEXT,
    ValueName TEXT,
    Value TEXT,
    DisplayName TEXT,
    Required BOOLEAN,
    ParentId INTEGER,
    FOREIGN KEY (PolicyElementId) REFERENCES PolicyElements(Id) ON DELETE CASCADE,
    FOREIGN KEY (ParentId) REFERENCES PolicyElementItems(Id) ON DELETE CASCADE
);

CREATE INDEX idx_items_elementid ON PolicyElementItems(PolicyElementId);
CREATE INDEX idx_items_parentid ON PolicyElementItems(ParentId);
CREATE INDEX idx_items_type ON PolicyElementItems(Type);

-- Таблица compliance
CREATE TABLE AgentPolicyCompliance (
    PolicyHash TEXT NOT NULL,
    UserSid TEXT,
    State TEXT NOT NULL,
    ActualValue TEXT,
    LastCheckedAt DATETIME NOT NULL,
    PRIMARY KEY (PolicyHash, UserSid)
);

-- Таблица версий
CREATE TABLE Versions(
    SourceRevision INTEGER NOT NULL,
    InstalledAt DATETIME NOT NULL
);