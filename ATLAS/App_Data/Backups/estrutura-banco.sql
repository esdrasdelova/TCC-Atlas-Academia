-- ============================================================
--  Atlas Centro de Treinamento — Script de criação do banco
--  MySQL / MariaDB
--
--  Como usar no MySQL Workbench:
--    1. File > Open SQL Script... (abra este arquivo)
--    2. Clique no raio (Execute) para rodar
--    3. Menu Database > Reverse Engineer...  → gera o diagrama EER
-- ============================================================

DROP DATABASE IF EXISTS atlas_db;
CREATE DATABASE atlas_db
    DEFAULT CHARACTER SET utf8mb4
    DEFAULT COLLATE utf8mb4_general_ci;
USE atlas_db;

-- ------------------------------------------------------------
--  Usuarios (hierarquia TPH: Aluno / PersonalTrainer / Admin)
-- ------------------------------------------------------------
CREATE TABLE Usuarios (
    Id                        INT           NOT NULL AUTO_INCREMENT,
    NomeCompleto              LONGTEXT      NOT NULL,
    Email                     VARCHAR(255)  NOT NULL,
    Telefone                  LONGTEXT      NOT NULL,
    SenhaHash                 LONGTEXT      NOT NULL,
    DataNascimento            DATETIME(6)   NOT NULL,
    Status                    INT           NOT NULL,
    CriadoEm                  DATETIME(6)   NOT NULL,
    PasswordResetToken        LONGTEXT      NULL,
    PasswordResetTokenExpires DATETIME(6)   NULL,
    Discriminator             VARCHAR(21)   NOT NULL,
    -- exclusivas de Aluno
    PersonalId                INT           NULL,
    Objetivo                  LONGTEXT      NULL,
    FotoUrl                   LONGTEXT      NULL,
    ProximaAvaliacao          DATETIME(6)   NULL,
    -- exclusivas de PersonalTrainer
    RegistroProfissional      LONGTEXT      NULL,
    Especialidade             LONGTEXT      NULL,
    CONSTRAINT PK_Usuarios PRIMARY KEY (Id),
    CONSTRAINT FK_Usuarios_Usuarios_PersonalId
        FOREIGN KEY (PersonalId) REFERENCES Usuarios (Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE UNIQUE INDEX IX_Usuarios_Email      ON Usuarios (Email);
CREATE        INDEX IX_Usuarios_PersonalId ON Usuarios (PersonalId);

-- ------------------------------------------------------------
--  Exercicios — catálogo/banco de exercícios
-- ------------------------------------------------------------
CREATE TABLE Exercicios (
    Id            INT          NOT NULL AUTO_INCREMENT,
    Nome          LONGTEXT     NOT NULL,
    Descricao     LONGTEXT     NULL,
    GrupoMuscular VARCHAR(255) NOT NULL,
    ImagemUrl     LONGTEXT     NULL,
    VideoUrl      LONGTEXT     NULL,
    Instrucoes    LONGTEXT     NULL,
    Ativo         TINYINT(1)   NOT NULL,
    CONSTRAINT PK_Exercicios PRIMARY KEY (Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX IX_Exercicios_GrupoMuscular ON Exercicios (GrupoMuscular);

-- ------------------------------------------------------------
--  Treinos — treino montado por um personal para um aluno
-- ------------------------------------------------------------
CREATE TABLE Treinos (
    Id          INT         NOT NULL AUTO_INCREMENT,
    AlunoId     INT         NOT NULL,
    PersonalId  INT         NOT NULL,
    Nome        LONGTEXT    NOT NULL,
    Objetivo    LONGTEXT    NULL,
    Observacoes LONGTEXT    NULL,
    DataCriacao DATETIME(6) NOT NULL,
    Publicado   TINYINT(1)  NOT NULL,
    Ativo       TINYINT(1)  NOT NULL,
    CONSTRAINT PK_Treinos PRIMARY KEY (Id),
    CONSTRAINT FK_Treinos_Usuarios_AlunoId
        FOREIGN KEY (AlunoId)    REFERENCES Usuarios (Id) ON DELETE CASCADE,
    CONSTRAINT FK_Treinos_Usuarios_PersonalId
        FOREIGN KEY (PersonalId) REFERENCES Usuarios (Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX IX_Treinos_AlunoId_Ativo ON Treinos (AlunoId, Ativo);
CREATE INDEX IX_Treinos_PersonalId    ON Treinos (PersonalId);

-- ------------------------------------------------------------
--  TreinoExercicios — itens que compõem um treino
-- ------------------------------------------------------------
CREATE TABLE TreinoExercicios (
    Id               INT        NOT NULL AUTO_INCREMENT,
    TreinoId         INT        NOT NULL,
    ExercicioId      INT        NOT NULL,
    Ordem            INT        NOT NULL,
    Series           INT        NOT NULL,
    Repeticoes       LONGTEXT   NOT NULL,
    Carga            LONGTEXT   NULL,
    DescansoSegundos INT        NOT NULL,
    Observacoes      LONGTEXT   NULL,
    CONSTRAINT PK_TreinoExercicios PRIMARY KEY (Id),
    CONSTRAINT FK_TreinoExercicios_Treinos_TreinoId
        FOREIGN KEY (TreinoId)    REFERENCES Treinos (Id)    ON DELETE CASCADE,
    CONSTRAINT FK_TreinoExercicios_Exercicios_ExercicioId
        FOREIGN KEY (ExercicioId) REFERENCES Exercicios (Id) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX IX_TreinoExercicios_TreinoId    ON TreinoExercicios (TreinoId);
CREATE INDEX IX_TreinoExercicios_ExercicioId ON TreinoExercicios (ExercicioId);

-- ------------------------------------------------------------
--  Agendamentos — avaliações e sessões marcadas
-- ------------------------------------------------------------
CREATE TABLE Agendamentos (
    Id         INT         NOT NULL AUTO_INCREMENT,
    AlunoId    INT         NOT NULL,
    PersonalId INT         NULL,
    Tipo       LONGTEXT    NOT NULL,
    DataHora   DATETIME(6) NOT NULL,
    Status     INT         NOT NULL,
    CONSTRAINT PK_Agendamentos PRIMARY KEY (Id),
    CONSTRAINT FK_Agendamentos_Usuarios_AlunoId
        FOREIGN KEY (AlunoId)    REFERENCES Usuarios (Id) ON DELETE CASCADE,
    CONSTRAINT FK_Agendamentos_Usuarios_PersonalId
        FOREIGN KEY (PersonalId) REFERENCES Usuarios (Id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX IX_Agendamentos_AlunoId    ON Agendamentos (AlunoId);
CREATE INDEX IX_Agendamentos_PersonalId ON Agendamentos (PersonalId);
CREATE INDEX IX_Agendamentos_DataHora   ON Agendamentos (DataHora);

-- ------------------------------------------------------------
--  NotificacoesLidas — controle de notificações vistas
-- ------------------------------------------------------------
CREATE TABLE NotificacoesLidas (
    Id      INT         NOT NULL AUTO_INCREMENT,
    AlunoId INT         NOT NULL,
    Chave   VARCHAR(255) NOT NULL,
    VistoEm DATETIME(6) NOT NULL,
    CONSTRAINT PK_NotificacoesLidas PRIMARY KEY (Id),
    CONSTRAINT FK_NotificacoesLidas_Usuarios_AlunoId
        FOREIGN KEY (AlunoId) REFERENCES Usuarios (Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE UNIQUE INDEX IX_NotificacoesLidas_AlunoId_Chave
    ON NotificacoesLidas (AlunoId, Chave(191));

-- ------------------------------------------------------------
--  Medidas — histórico de medidas corporais do aluno
-- ------------------------------------------------------------
CREATE TABLE Medidas (
    Id                INT           NOT NULL AUTO_INCREMENT,
    AlunoId           INT           NOT NULL,
    DataMedicao       DATETIME(6)   NOT NULL,
    PesoKg            DECIMAL(6,2)  NULL,
    AlturaCm          DECIMAL(6,2)  NULL,
    PeitoCm           DECIMAL(6,2)  NULL,
    CinturaCm         DECIMAL(6,2)  NULL,
    QuadrilCm         DECIMAL(6,2)  NULL,
    BracoCm           DECIMAL(6,2)  NULL,
    CoxaCm            DECIMAL(6,2)  NULL,
    PanturrilhaCm     DECIMAL(6,2)  NULL,
    PercentualGordura DECIMAL(5,2)  NULL,
    Observacoes       LONGTEXT      NULL,
    CONSTRAINT PK_Medidas PRIMARY KEY (Id),
    CONSTRAINT FK_Medidas_Usuarios_AlunoId
        FOREIGN KEY (AlunoId) REFERENCES Usuarios (Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX IX_Medidas_AlunoId_DataMedicao ON Medidas (AlunoId, DataMedicao);

-- ------------------------------------------------------------
--  Fotos — galeria de fotos de progresso do aluno
-- ------------------------------------------------------------
CREATE TABLE Fotos (
    Id        INT         NOT NULL AUTO_INCREMENT,
    AlunoId   INT         NOT NULL,
    Url       LONGTEXT    NOT NULL,
    Legenda   LONGTEXT    NULL,
    EnviadaEm DATETIME(6) NOT NULL,
    CONSTRAINT PK_Fotos PRIMARY KEY (Id),
    CONSTRAINT FK_Fotos_Usuarios_AlunoId
        FOREIGN KEY (AlunoId) REFERENCES Usuarios (Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX IX_Fotos_AlunoId_EnviadaEm ON Fotos (AlunoId, EnviadaEm);

-- ------------------------------------------------------------
--  Faturas — extrato financeiro da matrícula do aluno
-- ------------------------------------------------------------
CREATE TABLE Faturas (
    Id             INT           NOT NULL AUTO_INCREMENT,
    AlunoId        INT           NOT NULL,
    Descricao      LONGTEXT      NOT NULL,
    Valor          DECIMAL(10,2) NOT NULL,
    DataVencimento DATETIME(6)   NOT NULL,
    DataPagamento  DATETIME(6)   NULL,
    Status         INT           NOT NULL,
    CONSTRAINT PK_Faturas PRIMARY KEY (Id),
    CONSTRAINT FK_Faturas_Usuarios_AlunoId
        FOREIGN KEY (AlunoId) REFERENCES Usuarios (Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX IX_Faturas_AlunoId_DataVencimento ON Faturas (AlunoId, DataVencimento);

-- ------------------------------------------------------------
--  Frequencias — check-ins de treino (cálculo de presença)
-- ------------------------------------------------------------
CREATE TABLE Frequencias (
    Id         INT         NOT NULL AUTO_INCREMENT,
    AlunoId    INT         NOT NULL,
    DataTreino DATETIME(6) NOT NULL,
    TreinoNome LONGTEXT    NULL,
    CONSTRAINT PK_Frequencias PRIMARY KEY (Id),
    CONSTRAINT FK_Frequencias_Usuarios_AlunoId
        FOREIGN KEY (AlunoId) REFERENCES Usuarios (Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX IX_Frequencias_AlunoId_DataTreino ON Frequencias (AlunoId, DataTreino);
