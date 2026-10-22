# Documentação do Banco de Dados — Atlas Centro de Treinamento

**SGBD:** MySQL/MariaDB (XAMPP 10.4.32)
**Base de dados:** `atlas_db`
**Codificação:** `utf8mb4` / `utf8mb4_general_ci`
**ORM:** Entity Framework Core (Pomelo) — modelo *TPH* (Table-Per-Hierarchy)

---

## 1. Visão geral das tabelas

| Tabela | Registros (seed) | Descrição |
|---|---|---|
| `Usuarios` | 6 | Raiz da hierarquia de contas (Aluno / PersonalTrainer / Administrador) |
| `Exercicios` | 17 | Catálogo de exercícios (banco de exercícios) |
| `Treinos` | 3 | Treinos montados por um personal para um aluno |
| `TreinoExercicios` | 12 | Itens do treino (exercício + séries/reps/carga) |
| `Agendamentos` | 0 | Agendamentos de avaliação/sessão |
| `NotificacoesLidas` | 0 | Controle de notificações vistas por aluno |

---

## 2. Relacionamentos (chaves estrangeiras)

```
Usuarios (PersonalId) ──┐
                        └──→ Usuarios (Id)          [auto-relacionamento: aluno → personal]

Treinos (AlunoId)      ────→ Usuarios (Id)          [ON DELETE CASCADE]
Treinos (PersonalId)   ────→ Usuarios (Id)          [ON DELETE CASCADE]

TreinoExercicios (TreinoId) ───→ Treinos (Id)       [ON DELETE CASCADE]
TreinoExercicios (ExercicioId) → Exercicios (Id)    [ON DELETE RESTRICT]

Agendamentos (AlunoId)    ────→ Usuarios (Id)       [ON DELETE CASCADE]
Agendamentos (PersonalId) ────→ Usuarios (Id)       [ON DELETE SET NULL]

NotificacoesLidas (AlunoId) ──→ Usuarios (Id)       [ON DELETE CASCADE]
```

**Legenda das regras de exclusão:**
- `CASCADE` — se o pai for apagado, os filhos também são removidos
- `RESTRICT` — não permite apagar o pai enquanto houver filho (protege o catálogo de exercícios)
- `SET NULL` — o campo do filho é anulado (usado quando o personal é removido, mas o agendamento permanece)

---

## 3. Dicionário de dados

### 3.1 `Usuarios` (hierarquia TPH)

A coluna `Discriminator` define o tipo da conta. Aluno e Personal compartilham a mesma tabela (padrão TPH do EF Core), cada um preenchendo apenas as colunas que lhe interessam.

**Colunas comuns a todos os perfis:**

| Coluna | Tipo | Chave | Nulo | Descrição |
|---|---|---|---|---|
| `Id` | int | PK | não | Identificador único (auto incremento) |
| `NomeCompleto` | longtext | | não | Nome do usuário |
| `Email` | varchar(255) | UNIQUE | não | Login e destino de recuperação de senha |
| `Telefone` | longtext | | não | Telefone de contato |
| `SenhaHash` | longtext | | não | Hash PBKDF2 (SHA-256, 100.000 iterações) — nunca texto puro |
| `DataNascimento` | datetime(6) | | não | Data de nascimento |
| `Status` | int | | não | Situação da conta (enum `StatusConta`) |
| `CriadoEm` | datetime(6) | | não | Data de criação da conta |
| `PasswordResetToken` | longtext | | sim | Hash SHA-256 do token de redefinição (nunca o token em claro) |
| `PasswordResetTokenExpires` | datetime(6) | | sim | Expiração do link de recuperação (30 min) |
| `Discriminator` | varchar(21) | | não | Tipo: `Aluno`, `PersonalTrainer` ou `Administrador` |

**Colunas exclusivas do perfil Aluno:**

| Coluna | Tipo | Nulo | Descrição |
|---|---|---|---|
| `PersonalId` | int | sim | FK → `Usuarios.Id` do personal responsável |
| `Objetivo` | longtext | sim | Objetivo do aluno (hipertrofia, emagrecimento, etc.) |
| `FotoUrl` | longtext | sim | URL da foto do aluno |
| `ProximaAvaliacao` | datetime(6) | sim | Data da próxima avaliação física |

**Colunas exclusivas do perfil Personal:**

| Coluna | Tipo | Nulo | Descrição |
|---|---|---|---|
| `Especialidade` | longtext | sim | Área de especialização do personal |
| `RegistroProfissional` | longtext | sim | Número do registro (CREF) |

**Índices:**
- `IX_Usuarios_Email` — UNIQUE (garante um e-mail por conta)
- `IX_Usuarios_PersonalId` — acelera a carteira do personal

---

### 3.2 `Exercicios`

| Coluna | Tipo | Chave | Nulo | Descrição |
|---|---|---|---|---|
| `Id` | int | PK | não | Identificador (auto incremento) |
| `Nome` | longtext | | não | Nome do exercício |
| `Descricao` | longtext | | sim | Descrição / execução |
| `GrupoMuscular` | varchar(255) | IDX | não | Grupo muscular trabalhado |
| `ImagemUrl` | longtext | | sim | Imagem ilustrativa |
| `VideoUrl` | longtext | | sim | Vídeo demonstrativo |
| `Instrucoes` | longtext | | sim | Instruções técnicas |
| `Ativo` | tinyint(1) | | não | Se aparece no catálogo |

**Índice:** `IX_Exercicios_GrupoMuscular` — acelera filtros por grupo muscular.

---

### 3.3 `Treinos`

| Coluna | Tipo | Chave | Nulo | Descrição |
|---|---|---|---|---|
| `Id` | int | PK | não | Identificador (auto incremento) |
| `AlunoId` | int | FK | não | → `Usuarios.Id` (o aluno dono do treino) |
| `PersonalId` | int | FK | não | → `Usuarios.Id` (quem montou) |
| `Nome` | longtext | | não | Nome do treino (ex: "Treino A — Superiores") |
| `Objetivo` | longtext | | sim | Objetivo do treino |
| `Observacoes` | longtext | | sim | Observações gerais |
| `DataCriacao` | datetime(6) | | não | Data de criação |
| `Publicado` | tinyint(1) | | não | Se o aluno já pode ver |
| `Ativo` | tinyint(1) | | não | Treino vigente |

**Índices:** `IX_Treinos_AlunoId_Ativo` (treino ativo do aluno) e `IX_Treinos_PersonalId` (carteira do personal).

---

### 3.4 `TreinoExercicios` (itens do treino)

| Coluna | Tipo | Chave | Nulo | Descrição |
|---|---|---|---|---|
| `Id` | int | PK | não | Identificador (auto incremento) |
| `TreinoId` | int | FK | não | → `Treinos.Id` |
| `ExercicioId` | int | FK | não | → `Exercicios.Id` |
| `Ordem` | int | | não | Posição do exercício na sequência |
| `Series` | int | | não | Número de séries |
| `Repeticoes` | longtext | | não | Repetições (aceita "8-12", "até a falha") |
| `Carga` | longtext | | sim | Carga sugerida |
| `DescansoSegundos` | int | | não | Descanso entre séries |
| `Observacoes` | longtext | | sim | Observações do item |

**Índices:** `IX_TreinoExercicios_TreinoId` e `IX_TreinoExercicios_ExercicioId`.

---

### 3.5 `Agendamentos`

| Coluna | Tipo | Chave | Nulo | Descrição |
|---|---|---|---|---|
| `Id` | int | PK | não | Identificador (auto incremento) |
| `AlunoId` | int | FK | não | → `Usuarios.Id` |
| `PersonalId` | int | FK | sim | → `Usuarios.Id` |
| `Tipo` | longtext | | não | Tipo de agendamento |
| `DataHora` | datetime(6) | IDX | não | Data e hora marcada |
| `Status` | int | | não | Enum `StatusAgendamento` |

**Índices:** `IX_Agendamentos_AlunoId`, `IX_Agendamentos_PersonalId`, `IX_Agendamentos_DataHora`.

---

### 3.6 `NotificacoesLidas`

| Coluna | Tipo | Chave | Nulo | Descrição |
|---|---|---|---|---|
| `Id` | int | PK | não | Identificador (auto incremento) |
| `AlunoId` | int | FK | não | → `Usuarios.Id` |
| `Chave` | varchar(255) | UNIQUE* | não | Chave da notificação |
| `VistoEm` | datetime(6) | | não | Quando foi marcada como lida |

*Índice único composto `IX_NotificacoesLidas_AlunoId_Chave` — um registro por aluno + notificação.

---

## 4. Diagrama Entidade-Relacionamento

```
┌───────────────────────────────────────────┐
│                 Usuarios                  │
├───────────────────────────────────────────┤
│ PK  Id                                    │
│ UK  Email                                 │
│     NomeCompleto, Telefone, SenhaHash     │
│     DataNascimento, Status, CriadoEm      │
│     PasswordResetToken(Expires)           │
│     Discriminator                         │
│     -- Aluno --                           │
│ FK  PersonalId ───────────┐               │
│     Objetivo, FotoUrl,    │ (auto-relação)│
│     ProximaAvaliacao      │               │
│     -- Personal --        │               │
│     Especialidade,        │               │
│     RegistroProfissional  │               │
└───┬─────────┬─────────┬───┴───────────┬───┘
    │         │         │               │
    │ 1:N     │ 1:N     │ 1:N           │ 1:N
    │         │         │               │
    ▼         ▼         ▼               ▼
┌────────┐ ┌────────┐ ┌──────────────┐ ┌──────────────────┐
│Treinos │ │Agenda- │ │Notificacoes  │ │   (Personal)     │
│        │ │mentos  │ │Lidas         │ └──────────────────┘
├────────┤ ├────────┤ ├──────────────┤
│PK Id   │ │PK Id   │ │PK Id         │
│FK Alun.│ │FK Alun.│ │FK AlunoId    │
│FK Pers.│ │FK Pers.│ │UK Chave      │
│  Nome  │ │  Tipo  │ │  VistoEm     │
│Publica.│ │DataHora│ └──────────────┘
│ Ativo  │ │ Status │
└───┬────┘ └────────┘
    │ 1:N
    ▼
┌─────────────────────┐        ┌──────────────┐
│ TreinoExercicios    │ N:1    │  Exercicios  │
├─────────────────────┤───────►├──────────────┤
│ PK Id               │        │ PK Id        │
│ FK TreinoId         │        │   Nome       │
│ FK ExercicioId      │        │GrupoMuscular │
│   Ordem, Series     │        │   Ativo      │
│   Repeticoes, Carga │        │  Instrucoes  │
│   DescansoSegundos  │        └──────────────┘
└─────────────────────┘
```

---

## 5. Observações para a banca

1. **Segurança de senhas** — nenhuma senha é armazenada em texto puro. O `SenhaHash` usa **PBKDF2-SHA256 com 100.000 iterações** e *salt* aleatório.

2. **Recuperação de senha** — o banco guarda apenas o **hash SHA-256** do token. O token em claro trafega somente no link enviado por e-mail, expira em 30 minutos e é de uso único.

3. **Herança TPH** — Aluno, Personal e Administrador ficam na mesma tabela `Usuarios`, separados pela coluna `Discriminator`. Isso simplifica o login (uma única consulta) e mantém a integridade dos relacionamentos.

4. **Integridade referencial** — todas as chaves estrangeiras estão declaradas no banco, com regras de exclusão adequadas ao domínio (cascata para dados dependentes, *restrict* para o catálogo de exercícios).

5. **Performance** — os índices foram criados a partir das consultas mais frequentes: login por e-mail, carteira do personal, treino ativo do aluno e filtros de agendamento.
