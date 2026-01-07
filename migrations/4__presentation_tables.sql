-- Таблица представлений
CREATE TABLE Presentations (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,

    PresentationId TEXT NOT NULL,   -- id из ADML
    AdmlFile TEXT NOT NULL,          -- имя adml

    UNIQUE (PresentationId, AdmlFile)
);

CREATE INDEX idx_presentations_pid ON Presentations(PresentationId);

-- Таблица элементов управления политикой
CREATE TABLE PresentationElements (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PresentationId INTEGER NOT NULL,
    ElementType TEXT NOT NULL
        CHECK (ElementType IN (
            'dropdownList',
            'text',
            'checkBox',
            'listBox',
            'textBox',
            'multiTextBox',
            'decimalTextBox',
            'label',
            'defaultValue'
        )),

    RefId TEXT,                      -- refId -> PolicyElements.ElementId (логическая связь)
    ParentElementId INTEGER,        -- ТОЛЬКО для label
    DefaultValue TEXT,              -- значение defaultValue как дочернего элемента textBox,
    DisplayOrder INTEGER NOT NULL DEFAULT 0,

    FOREIGN KEY (PresentationId)
        REFERENCES Presentations(Id)
        ON DELETE CASCADE,

    FOREIGN KEY (ParentElementId)
        REFERENCES PresentationElements(Id)
        ON DELETE CASCADE
);

CREATE INDEX idx_pe_pres ON PresentationElements(PresentationId);
CREATE INDEX idx_pe_type ON PresentationElements(ElementType);
CREATE INDEX idx_pe_refid ON PresentationElements(RefId);

-- таблица аттрибутов элементов представления
CREATE TABLE PresentationElementAttributes (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,

    PresentationElementId INTEGER NOT NULL,
    Name TEXT NOT NULL,
    Value TEXT NOT NULL,

    FOREIGN KEY (PresentationElementId)
        REFERENCES PresentationElements(Id)
        ON DELETE CASCADE,

    UNIQUE (PresentationElementId, Name)
);

CREATE INDEX idx_pea_elem ON PresentationElementAttributes(PresentationElementId);

-- таблица переводов для эдменетов представления
CREATE TABLE PresentationTranslations (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,

    PresentationElementId INTEGER NOT NULL,
    LangCode TEXT NOT NULL,              -- ru-RU, en-US
    TextValue TEXT NOT NULL COLLATE NOCASE,

    FOREIGN KEY (PresentationElementId)
        REFERENCES PresentationElements(Id)
        ON DELETE CASCADE,

    UNIQUE (PresentationElementId, LangCode)
);

CREATE INDEX idx_pt_lang ON PresentationTranslations(LangCode);
CREATE INDEX idx_pt_elem ON PresentationTranslations(PresentationElementId);