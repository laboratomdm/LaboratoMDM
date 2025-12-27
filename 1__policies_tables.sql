-- Таблица уникальных политик
CREATE TABLE Policies (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Scope TEXT NOT NULL DEFAULT 'None', -- User, Machine, Both, None
    RegistryKey TEXT NOT NULL DEFAULT '',
    ValueName TEXT NOT NULL DEFAULT '',
    EnabledValue INTEGER,
    DisabledValue INTEGER,
    SupportedOnRef TEXT,
    ParentCategoryRef TEXT,
    PresentationRef TEXT,
    Hash TEXT NOT NULL, -- хэш политики (для идентичности)
    CONSTRAINT chk_scope CHECK(Scope IN ('None', 'User', 'Machine', 'Both')),
    UNIQUE(Name, Hash)
);

CREATE INDEX idx_policies_name ON Policies(Name);
CREATE INDEX idx_policies_scope ON Policies(Scope);
CREATE INDEX idx_policies_supportedon ON Policies(SupportedOnRef);
CREATE INDEX idx_policies_hash ON Policies(Hash);
CREATE INDEX idx_policies_parentcategory ON Policies(ParentCategoryRef);

-- Таблица элементов политики (elements)
CREATE TABLE PolicyElements (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PolicyId INTEGER NOT NULL,
    ElementId TEXT NOT NULL,      -- внутренний id элемента в ADMX
    Type TEXT NOT NULL,           -- text, decimal, checkbox, list, combobox
    ValueName TEXT,
    Required INTEGER NOT NULL DEFAULT 0,
    MaxLength INTEGER,
    ClientExtension TEXT,
    FOREIGN KEY (PolicyId) REFERENCES Policies(Id) ON DELETE CASCADE,
    UNIQUE(PolicyId, ElementId)
);

CREATE INDEX idx_elements_policyid ON PolicyElements(PolicyId);
CREATE INDEX idx_elements_type ON PolicyElements(Type);
CREATE INDEX idx_elements_valuename ON PolicyElements(ValueName);

-- Таблица загруженных ADMX файлов
CREATE TABLE AdmxFiles (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FileName TEXT NOT NULL,
    FileHash TEXT NOT NULL,       -- хэш содержимого файла (SHA256)
    LoadedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(FileHash)
);

CREATE INDEX idx_admxfiles_filename ON AdmxFiles(FileName);
CREATE INDEX idx_admxfiles_hash ON AdmxFiles(FileHash);

-- Пространства имен используемые в ADMX файле
CREATE TABLE PolicyNamespaces (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Prefix TEXT NOT NULL,
    Namespace TEXT NOT NULL,
    AdmxFileId INTEGER NOT NULL,
    FOREIGN KEY (AdmxFileId) REFERENCES AdmxFiles(Id) ON DELETE CASCADE,
    UNIQUE(Prefix, AdmxFileId)
);

CREATE INDEX idx_namespaces_prefix ON PolicyNamespaces(Prefix);
CREATE INDEX idx_namespaces_admxfile ON PolicyNamespaces(AdmxFileId);

-- Категории политик из конкретного ADMX файла
CREATE TABLE PolicyCategories (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL UNIQUE,
    DisplayName TEXT NOT NULL,
    ExplainText TEXT,
    ParentCategoryRef TEXT
);

CREATE INDEX idx_categories_name ON PolicyCategories(Name);

-- Связь политики с исходным ADMX файлом
CREATE TABLE PolicyAdmxMapping (
    PolicyId INTEGER NOT NULL,
    AdmxFileId INTEGER NOT NULL,
    PRIMARY KEY (PolicyId, AdmxFileId),
    FOREIGN KEY (PolicyId) REFERENCES Policies(Id) ON DELETE CASCADE,
    FOREIGN KEY (AdmxFileId) REFERENCES AdmxFiles(Id) ON DELETE CASCADE
);

CREATE INDEX idx_mapping_policyid ON PolicyAdmxMapping(PolicyId);
CREATE INDEX idx_mapping_admxfileid ON PolicyAdmxMapping(AdmxFileId);

-- Таблица зависимостей возможностей ОС
CREATE TABLE PolicyCapabilities (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PolicyId INTEGER NOT NULL,
    Capability TEXT NOT NULL,
    FOREIGN KEY (PolicyId) REFERENCES Policies(Id) ON DELETE CASCADE,
    UNIQUE(PolicyId, Capability)
);

CREATE INDEX idx_policy_capability ON PolicyCapabilities(PolicyId);
CREATE INDEX idx_capability ON PolicyCapabilities(Capability);

-- Таблица зависимостей железа
CREATE TABLE PolicyHardwareRequirements (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PolicyId INTEGER NOT NULL,
    HardwareFeature TEXT NOT NULL,
    FOREIGN KEY (PolicyId) REFERENCES Policies(Id) ON DELETE CASCADE,
    UNIQUE(PolicyId, HardwareFeature)
);

CREATE INDEX idx_policy_hardware ON PolicyHardwareRequirements(PolicyId);
CREATE INDEX idx_hardwarefeature ON PolicyHardwareRequirements(HardwareFeature);