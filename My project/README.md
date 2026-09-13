# NetShift: Protocolo Quântico

<div align="center">

![Unity 6](https://img.shields.io/badge/Unity-6000.3.6f1-black?style=for-the-badge&logo=unity)
![C#](https://img.shields.io/badge/C%23-10.0%20%2F%20.NET-blue?style=for-the-badge&logo=csharp)
![Genre](https://img.shields.io/badge/Gênero-Tactical%20SRPG%20%2F%20Cyberpunk-8A2BE2?style=for-the-badge)
![Status](https://img.shields.io/badge/Status-Em%20Desenvolvimento-00FF66?style=for-the-badge)
![Studio](https://img.shields.io/badge/Estúdio-Neotopia%20Games-orange?style=for-the-badge)

**Um RPG Tático em Turnos (SRPG) que combina investigação urbana cyberpunk com batalhas em grade 3D, fusões de inteligências autônomas em tempo real e vínculos de rede.**

[Visão Geral](#-visão-geral) •
[Pilares de Gameplay](#-pilares-de-gameplay) •
[Sistemas de Combate](#-sistemas-de-combate) •
[Fusão e App-Link](#-sistema-de-fusão-e-app-link) •
[Personagens](#-personagens-principais) •
[Interface e UX](#-interface-e-experiência-do-usuário) •
[Arquitetura](#-arquitetura-do-projeto) •
[Controles](#-controles) •
[Como Executar](#-como-executar-o-projeto)

---

</div>

## 🌐 Visão Geral

### Sinopse
Em **2032**, a sociedade global opera inteiramente sustentada pela **AETHER-NET**, uma infraestrutura de rede quântica onipresente. Quando uma série de misteriosas anomalias e casos de "comatose digital" atingem usuários em Tóquio, descobre-se que aplicativos, algoritmos e inteligências artificiais ganharam consciência própria no submundo da rede: o **CYBER-SPACE**.

No papel de **Ren Kaiba**, um jovem investigador cibernético da **Agência Hudie**, você deve alternar entre a exploração investigativa nas ruas de Tóquio (Shinjuku, Akihabara, Nakano) e incursões táticas na grade virtual para conter entidades corrompidas e impedir o colapso da infraestrutura humana.

### High Concept
> *"E se os aplicativos e inteligências artificiais do nosso mundo ganhassem consciência em uma dimensão digital oculta, e a única forma de salvar a infraestrutura da humanidade fosse liderar batalhas táticas em uma grade virtual fundindo entidades em tempo real?"*

---

## 🎮 Pilares de Gameplay

O jogo sintetiza três referências aclamadas do gênero com identidade autoral:

1. **Investigação Urbana & Hacking Dimensional** *(Ref: Digimon Story: Cyber Sleuth)*:
   - Transição dinâmica entre o Mundo Real e o CYBER-SPACE através de **Access Points** e terminais físicos.
   - Habilidades de Campo (*Decrypt Break*, *Path Reveal*, *Ghost Protocol*) desbloqueadas pelas classes das entidades na equipe.
2. **Combate Tático em Grade Isométrica 3D** *(Ref: Digimon Survive)*:
   - Batalhas por turnos orientadas por iniciativa e velocidade.
   - Relevo e elevação de terreno afetando alcance, precisão e dano.
   - Direcionamento estratégico: **Dano Frontal (100%)**, **Lateral (125%)** e **Traseiro / Backstab (150% + Crítico garantido)**.
3. **Combate em Duplas e Fusões Instantâneas** *(Ref: Digimon Universe: Appli Monsters)*:
   - Formação em duplas (2v2) com ataques sincronizados (*Combo Guard Break*).
   - Sistema de fusão em tempo real (**NetFusion / App Gappai**) e vínculos de passivas (**App-Link**).
   - **Deck de Programas** acionado pelo operador humano em tempo real sem consumir o turno da criatura.

---

## ⚔️ Sistemas de Combate

### 1. O Ciclo dos 7 Atributos Funcionais
As entidades digitais pertencem a categorias operacionais que formam um ciclo tático contínuo de vantagens de dano:

```
[ Social ] ──(vence)──▶ [ Navi ] ──(vence)──▶ [ Tool ]
    ▲                                              │
 (vence)                                        (vence)
    │                                              ▼
[ System ] ◀──(vence)── [ Life ] ◀──(vence)── [ Entertainment ] ◀──(vence)── [ Game ]
```

| Categoria | Especialidade Tática | Vantagem Direta Contra |
|---|---|---|
| **Social** | Suporte de grupo, manipulação e controle de multidão (CC) | **Navi** |
| **Navi** | Alta precisão, mobilidade, evasão e teletransporte | **Tool** |
| **Tool** | Tanque robusto, impacto físico pesado e controle de terreno | **Game** |
| **Game** | Golpes críticos de alto risco, multi-hit e acertos em cadeia | **Entertainment** |
| **Entertainment** | Debuffs incapacitantes, indução de status e provocação (Taunt) | **Life** |
| **Life** | Regeneração contínua, cura em área e suporte biológico | **System** |
| **System** | Negação de código, dano puro e quebra de barreiras digitais | **Social** |
| *Security* | Proteção de barreira e contenção de malware | Equilibrado / Neutro |

---

### 2. Trindade de Protocolo (Karma)
Suas escolhas éticas durante a investigação moldam a evolução da sua entidade principal:

- 🛡️ **Firewall (Moral / Proteção)**: Ordem, altruísmo, retidão e aumento defensivo.
- ⚖️ **Ping (Harmonia / Equilíbrio)**: Pragmatismo, diplomacia, estabilidade e suporte versátil.
- ⚡ **Overclock (Cólera / Ruptura)**: Impulso, rebelião, agressividade e foco em dano extremo.

---

## 🧬 Sistema de Fusão e App-Link

### 1. Fusão em Batalha (NetFusion / App Gappai)
Quando dois monstros compatíveis estão presentes no campo de batalha dentro de um **raio tático de 4x4 quadros**, a opção de **Fusão** é liberada no menu de ações:

- **Execução Instantânea**: Os dois combatentes se combinam no tile do monstro ativo, gerando uma nova criatura de rank superior (*Super* ou *Ultimate*).
- **Recuperação Imediata**: A fusão restaura vida, limpa penalidades e reseta cooldowns de habilidades.
- **Validade Temporária da Batalha**: A entidade fundida permanece em campo até o fim do combate. Ao concluir o confronto, o sistema reverte automaticamente a fusão e faz o respawn dos dois combatentes originais com seus respectivos status preservados.
- **Regra de Compatibilidade Estrita**: Apenas combinações canônicas homologadas no compêndio do jogo são permitidas. Pares incompatíveis têm a confirmação bloqueada sem consumo de turno.

#### 📜 Receitas Canônicas Homologadas

```mermaid
graph TD
    DV[Data-Viper\nStandard] + SH[Shitakumon\nStandard] --> HV[Hydro-Vipermon\nSuper]
    GH[Glitch-Hound\nStandard] + SB[Sound-Beat\nStandard] --> SD[Sonic-Debugger\nSuper]
    CC[Craft-Craft\nStandard] + DV2[Data-Viper\nStandard] --> AR[Architectmon\nSuper]
    FL[Flame-Log\nStandard] + CC2[Craft-Craft\nStandard] --> ML[Magma-Logmon\nSuper]
    VP[Volt-Plug\nStandard] + SC[Shadow-Cam\nStandard] --> EC[Electro-Cammon\nSuper]
    BP[Bio-Patch\nStandard] + MC[Magnet-Core\nStandard] --> BM[Bio-Magnetmon\nSuper]

    HV + AR --> PV[Poseidon-Vipermon\nUltimate]
    SD + EC --> OD[Omega-Debugger\nUltimate]
    AR + ML --> DN[Dreadnoughtmon\nUltimate]
```

---

### 2. Sistema de App-Link de Mochila e Campo
Permite vincular uma criatura secundária da mochila (*Bag Appmon*) a um combatente ativo em campo:
- **Transferência de Passivas**: O combatente principal herda a passiva exclusiva da entidade secundária (ex: *Cód. Defensivo*, *Instabilidade*, *Amp Sônico*).
- **Bônus de Atributos**: Concede amplificação percentual em ATK, DEF, SPD ou Crítico conforme a afinidade.
- **HUD Integrado**: Exibe o badge da criatura vinculada e seus modificadores ativos diretamente abaixo do card do personagem.

---

## 👥 Personagens Principais

| Personagem | Papel | Descrição |
|---|---|---|
| **Ren Kaiba** (18 anos) | Protagonista / Operador de Campo | Detetive da Agência Hudie que teve sua percepção alterada por um vazamento quântico, permitindo enxergar anomalias digitais a olho nu. Controla a movimentação, táticas e o Deck de Programas sem gastar turnos das criaturas. |
| **Aethel** | Entidade Parceira Principal | Criatura misteriosa da categoria *System* resgatada de um arquivo corrompido sob Nakano. Possui a habilidade única de assimilar código via NetFusion, ramificando suas formas conforme as escolhas de Karma do jogador. |
| **Maya Tachibana** (24 anos) | Mentora Hacker | Líder da Agência Hudie e ex-engenheira da Kamishiro Enterprise. Fornece suporte tático no HUB (Quantum-Lab), vende upgrades do Deck e desbloqueia Habilidades de Campo. |

---

## 🖥️ Interface e Experiência do Usuário

O jogo conta com interfaces ricas, responsivas e desenhadas com estética cibernética neopunk:

* **Battle HUD Estilo Digimon Survive**:
  - Resolução de referência nativa de **800x600** com escalonamento nítido e responsivo para telas Full HD e 4K.
  - Card de status tático com retrato, barras de HP/MP, atributos e Timeline de Turnos ativa.
  - Menu de ações dinâmico com navegação por teclado e botões integrados: `[Mover]`, `[Ataque]`, `[Item]`, `[Evolução / Gappai]`, `[Link]`, `[Passar]`.
* **Painel de Itens e Habilidades Simétrico (730 x 285 px)**:
  - Layout equilibrado em duas colunas com 35px de margem em cada borda (sem cortes laterais).
  - Aba de ação no estilo pasta/índice no topo da lista.
  - Mini-grids interativos de **Alcance** e **Área de Efeito (AoE)** com indicador de elevação.
  - Quebra de linha inteligente para descrições de consumíveis e habilidades.
* **Cockpit Holográfico de Fusão (`AppGappaiMenuUI`)**:
  - Chassi cibernético de 1300x910 px com brilho ciano neon (`#00D4FF`) e molduras chanfradas.
  - **Slot Duplo no Topo**: Monstro Principal (`MAIN`) e Parceiro Selecionado (`PARTNER`).
  - **Conector Central Digital**: Cápsula de fusão com animação de pulso neon e display do rank gerado.
  - **Grade Tática 4x4**: Exibe os aliados presentes no raio de 4 quadros com retratos, mini-barras de HP e tags de compatibilidade (`★ Gappai OK` ou `✖ Incompatível`).
  - **Blueprint Deck**: Painel holográfico de pré-visualização completa com comparação de status, habilidades herdadas e fórmulas recomendadas.
* **Menu de App-Link (`AppLinkMenuUI`)**:
  - Interface dedicada para gerenciamento de links da mochila, ativação de chips de dados e visualização de sinergias.
* **Menu de Desdobramento Pré-Batalha (`DeploymentMenuUI`)**:
  - Seleção de unidades e posicionamento inicial antes do início do combate tático.

---

## 🏗️ Arquitetura do Projeto

A base de código em **C#** segue boas práticas de engenharia de software, orientada a componentes modulares e desacoplamento:

```
Assets/
├── Scenes/
│   └── SampleScene.unity            # Cena principal de batalha e integração
├── Scripts/
│   ├── Board/                       # Lógica de grid 3D, tiles procedurais e destaque
│   │   ├── Board.cs
│   │   ├── Tile.cs
│   │   ├── GridHighlighter.cs       # Renderização de alcance, área e Gappai 4x4
│   │   └── ProceduralGridTileFactory.cs
│   ├── Combat/                      # Controle de combate, turnos e cálculos
│   │   ├── BattleController.cs
│   │   ├── TurnController.cs
│   │   └── DamageCalculator.cs
│   ├── State Machine/               # Máquina de Estados Finita (FSM) tática
│   │   ├── BattleStateMachine.cs
│   │   └── States/
│   │       ├── InitBattleState.cs
│   │       ├── ChooseActionState.cs
│   │       ├── MoveTargetState.cs
│   │       ├── AttackTargetState.cs
│   │       ├── SelectItemState.cs
│   │       ├── AppGappaiState.cs    # Estado dedicado à seleção e fusão 4x4
│   │       └── EndTurnState.cs
│   ├── TacticalBattle/              # Regras de domínio e dados táticos
│   │   ├── Appmon/
│   │   │   ├── AppmonCharacter.cs
│   │   │   ├── AppmonDatabase.cs    # Compêndio oficial de monstros e golpes
│   │   │   └── AppGappaiData.cs     # Tabela canônica e serviço AppGappaiService
│   │   ├── AppLink/
│   │   │   └── AppLinkService.cs    # Gestão de vínculos de mochila e buffs
│   │   └── Tests/
│   │       └── AppmonAndComboTestSuite.cs # Suíte de testes automatizados
│   ├── UI/                          # Interfaces procedurais de usuário
│   │   ├── BattleHUD.cs             # HUD principal de batalha e seletores
│   │   ├── AppGappaiMenuUI.cs       # Cockpit de fusão 4x4
│   │   ├── AppLinkMenuUI.cs         # Menu de vínculos e passivas
│   │   ├── DeploymentMenuUI.cs      # Menu de desdobramento de equipe
│   │   └── DamagePopupService.cs    # Popups de dano e efeitos visuais
│   └── Unit/                        # Entidades de combate, atributos e movimentação
│       ├── Unit.cs
│       ├── UnitStats.cs
│       └── Movement.cs
```

### Destaques Técnicos
- **FSM Estrita**: O fluxo de batalha opera via estados isolados (`BattleStateMachine`), garantindo transições determinísticas entre movimentação, mira de habilidades, seleção de itens e fusão.
- **UI Procedural Sem Dependência de Prefabs Pesados**: Ícones, barras de progresso, botões estilizados e molduras neon são gerados proceduralmente em runtime via Unity UI, mantendo o projeto leve e reprodutível.
- **Testes Automatizados**: Contém testes unitários em [`AppmonAndComboTestSuite.cs`](Assets/Scripts/TacticalBattle/Tests/AppmonAndComboTestSuite.cs) cobrindo ciclo de atributos, combos sincronizados, tabelas de fusão e restauração pós-batalha.

---

## ⌨️ Controles

| Ação | Teclado | Mouse / Controle |
|---|---|---|
| **Mover Cursor / Navegar Menus** | `W`, `A`, `S`, `D` ou `Setas Direcionais` | Clique no Tile / D-Pad |
| **Confirmar / Executar Ação** | `Espaço`, `Enter` ou `Z` | Clique Esquerdo / Botão Sul (A) |
| **Cancelar / Voltar / Modo Livre** | `ESC` ou `X` | Clique Direito / Botão Leste (B) |
| **Alternar Abas / Alvos** | `Q` / `E` ou `Tab` | Roda do Mouse / Bumpers (LB/RB) |
| **Girar Câmera Tática** | `Page Up` / `Page Down` | Botão Central do Mouse |

---

## 🚀 Como Executar o Projeto

### Pré-requisitos
- **Unity 6** (Recomendado: `Unity 6000.3.6f1` ou superior).
- **Unity Hub**.
- Módulo de suporte a **PC / Mac Standalone**.

### Passo a Passo
1. Clone o repositório em sua máquina:
   ```bash
   git clone https://github.com/alexsander020/Jogo.git
   ```
2. Abra o **Unity Hub**, clique em **Add** (Adicionar projeto do disco) e selecione a pasta `My project`.
3. Abra o projeto utilizando o **Unity 6000.3.6f1**.
4. No painel de arquivos (*Project*), navegue até `Assets/Scenes/` e abra a cena **`SampleScene.unity`**.
5. Clique no botão **Play (▶)** no topo da janela do Unity para iniciar a batalha tática.

### Executando os Testes Automatizados
1. No Unity Editor, abra a janela de testes em:
   `Window` ➔ `General` ➔ `Test Runner`.
2. Selecione a aba **EditMode** ou **PlayMode**.
3. Localize a suíte `TacticalBattle.Tests.AppmonAndComboTestSuite` e clique em **Run All**.

---

## 📄 Créditos e Referências

- **Desenvolvimento & Game Design**: Neotopia Games Studio / alexsander020.
- **Inspirações e Referências Artísticas/Mecânicas**:
  - *Digimon Survive* (Bandai Namco / Hyde) — Grid tático, relevo e direcionamento.
  - *Digimon Story: Cyber Sleuth – Hacker's Memory* (Media.Vision) — Investigação urbana e estética cyberpunk.
  - *Digimon Universe: Appli Monsters* (Bandai / Inti Creates) — Sistema de atributos funcionais, App-Link e fusões.
- **Engine**: Desenvolvido sobre o ecossistema [Unity](https://unity.com/).

---

<div align="center">

*NetShift: Protocolo Quântico © 2026. Todos os direitos reservados à Neotopia Games Studio.*

</div>
