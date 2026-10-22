# Roteiro de Apresentação — Atlas Centro de Treinamento (TCC)

> **Tempo estimado:** 12 a 15 minutos
> **Objetivo:** demonstrar o site institucional + a plataforma de gestão com os 3 perfis de acesso

---

## Preparação (faça ANTES de começar)

### 1. Ligue o banco
Abra o **XAMPP Control Panel** → clique em **Start** em **MySQL**.
> ⚠️ Sem isso o site não sobe. Confirme que ficou verde.

### 2. Rode o projeto
Abra um terminal na pasta do projeto e execute:

```powershell
dotnet run --project ATLAS\ATLAS.csproj
```

Espere aparecer:
```
Now listening on: http://localhost:5078
Application started. Press Ctrl+C to shut down.
```

### 3. Abra o navegador
Acesse **http://localhost:5078**

### 4. Deixe pronto para mostrar o banco (opcional, mas impacta)
Abra em outra aba o **phpMyAdmin**: http://localhost/phpmyadmin → banco `atlas_db`

### 5. Tenha as contas à mão

| Perfil | E-mail | Senha |
|---|---|---|
| Administrador | `admin@atlasct.com.br` | `123456` |
| Personal | `carlos.mendes@atlasct.com.br` | `123456` |
| Aluno | `joao.silva@email.com` | `123456` |

---

## Bloco 1 — Site institucional (2 min)

**Rota:** `/` (Home)

Mostre e comente:

- **Home** — apresentação da academia, destaques (+500 alunos, +15 treinadores), horários
- **Sobre** — história e equipe
- **Modalidades** — musculação, funcional, pilates, fisioterapia
- **Planos** — planos oferecidos
- **Contato** — formulário + mapa + endereço

> 💬 *"O site institucional serve como vitrine e canal de contato, além de dar acesso à plataforma."*

**Destaque:** clique no botão flutuante **"Precisa de ajuda?"** (canto inferior)
> 💬 *"Este é o assistente local: responde dúvidas sobre treino e saúde sem depender de nenhuma API externa."*

---

## Bloco 2 — Cadastro e login (2 min)

### Cadastro
**Rota:** `/cadastro`

Cadastre um aluno novo na frente da banca. Use um e-mail diferente dos existentes, ex:
`aluno.teste@email.com` / senha `123456`

> 💬 *"A senha nunca é gravada em texto puro — usamos PBKDF2 com 100 mil iterações."*

### Login
**Rota:** `/login`

Se quiser provar o armazenamento, abra o phpMyAdmin depois e mostre a coluna `SenhaHash` do novo usuário.

---

## Bloco 3 — Área do Aluno (3 min)

**Login:** `joao.silva@email.com` / `123456`

| Tela | Rota | O que mostrar |
|---|---|---|
| Dashboard | `/aluno` | Treino ativo do dia, resumo |
| Meus Treinos | `/aluno/treinos` | Lista + detalhe do treino com exercícios, séries, repetições e carga |
| Exercícios | `/aluno/exercicios` | Catálogo com instruções e vídeos |
| Notificações | `/aluno/notificacoes` | Avisos do personal |
| Perfil | `/aluno/perfil` | Editar dados e trocar foto |

> 💬 *"O aluno só enxerga o que foi publicado para ele, atendendo à regra de menor privilégio."*

---

## Bloco 4 — Área do Personal (4 min) ⭐ **bloco mais forte**

**Login:** `carlos.mendes@atlasct.com.br` / `123456`

| Tela | Rota | O que mostrar |
|---|---|---|
| Dashboard | `/personal` | Visão geral da carteira |
| Meus Alunos | `/personal/alunos` | Só os alunos vinculados a ele |
| **Criar Treino** | `/personal/treinos/novo` | ⭐ **Monte um treino ao vivo** |
| Banco de Exercícios | `/personal/banco-exercicios` | 17 exercícios por grupo muscular |

### Roteiro do treino ao vivo (o momento alto)

1. Escolha o aluno (ex: João Silva)
2. Dê um nome ao treino (ex: "Treino C — Pernas")
3. Adicione 3 exercícios (ex: Agachamento, Leg Press, Cadeira Extensora)
4. Defina séries, repetições, carga e descanso
5. Salve
6. Clique em **Publicar** e depois **Ativar**

> 💬 *"Cada personal enxerga apenas seus próprios alunos e treinos — o escopo é aplicado no servidor, não só na tela."*

**Prova visual:** va em `/aluno/treinos` (login do aluno) e mostre o treino que você acabou de criar. A banca adora isso. 😄

---

## Bloco 5 — Área Administrativa (3 min)

**Login:** `admin@atlasct.com.br` / `123456`

| Tela | Rota | O que mostrar |
|---|---|---|
| Dashboard | `/admin` | Indicadores reais do sistema |
| Alunos | `/admin/alunos` | CRUD completo (criar, editar, ativar/desativar, excluir) |
| Personais | `/admin/personais` | CRUD completo |
| Treinos | `/admin/treinos` | Todos os treinos do sistema |
| Configurações | `/admin/configuracoes` | Dados do site |

**Regra interessante pra citar:** o admin **não consegue desativar nem excluir a própria conta** enquanto estiver logado.

---

## Bloco 6 — Recuperação de senha (2 min) ⭐ **impacto garantido**

1. Faça logout
2. Vá em `/esqueci-senha`
3. Digite um e-mail cadastrado
4. Abra a caixa de entrada e mostre o **e-mail real recebido** com o botão "Redefinir minha senha"
5. Clique no botão, defina nova senha, faça login

> 💬 *"O link tem token único, expira em 30 minutos e só pode ser usado uma vez. No banco guardamos apenas o hash do token, nunca o link em claro."*

> 💬 *"A resposta na tela é sempre genérica, pra não revelar quais e-mails existem no sistema."*

---

## Bloco 7 — Segurança (1 min) — bom de citar

- **Senhas:** PBKDF2-SHA256, 100.000 iterações, com salt
- **Sessão:** autenticação por cookie com expiração de 8h e renovação deslizante
- **Força bruta:** bloqueio temporário após várias tentativas de login erradas
- **Recuperação:** token de uso único, validade de 30 min, anti-spam por e-mail e IP
- **Autorização:** cada área protegida por papel (`[Authorize(Roles=...)]`)
- **CSRF:** token antiforgery em todos os formulários

---

## Plano de contingência

| Problema | Solução rápida |
|---|---|
| Site não abre | O MySQL do XAMPP está ligado? A porta 5078 está livre? |
| Erro de conexão com banco | Reinicie o MySQL no XAMPP e rode o projeto de novo |
| Perdi um dado criado na demo | Não importa — é ambiente de demonstração |
| Tela travou | F5 (recarregar). As sessões são por cookie |
| Quer resetar tudo | Restaure o backup: veja abaixo |

### Restaurar o backup

```powershell
Get-Content ATLAS\App_Data\Backups\atlas_db_*.sql | & "C:\xampp\mysql\bin\mysql" -u root
```

---

## Checklist final (5 min antes da banca)

- [ ] MySQL ligado no XAMPP
- [ ] `dotnet run` executado e site respondendo
- [ ] Navegador aberto em `http://localhost:5078`
- [ ] phpMyAdmin aberto numa aba (para mostrar o banco)
- [ ] Caixa de e-mail aberta (para a demo de recuperação)
- [ ] Contas de teste anotadas
- [ ] Terminal minimizado (log aparece ali, mas não precisa mostrar)
- [ ] Notificações do sistema silenciadas
- [ ] Backup `.sql` à mão, caso precise resetar
