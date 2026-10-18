# Setup Automático — Montar Cena em 1 Clique

Você tem duas opções: **automática** (recomendada) ou **manual**.

## ✅ Opção 1: Automática (Recomendada)

**Arquivo criado:** `Assets/Editor/SceneSetup.cs`

Este script monta toda a cena automaticamente.

### Como Usar:

1. Abra o projeto no Unity (abrirá em Play mode se já estiver rodando)
2. No menu superior, clique: **Farty Farm** → **Setup Arena Scene**
3. Aguarde (leva ~3-5 segundos)
4. A cena será salva automaticamente em `Assets/Scenes/Arena.unity`
5. Console mostrará: ✓ "Arena scene criada com sucesso: Assets/Scenes/Arena.unity"

Pronto! A Etapa 1 está 100% montada e salva.

### O que o script cria:

- ✓ Cena: `Arena.unity`
- ✓ Ground (chão verde 20x20)
- ✓ Walls (4 paredes cinzas)
- ✓ Boi (corpo, cabeça, olhos, partículas)
- ✓ MainCamera (3ª pessoa perspectiva)
- ✓ GameManager (UI)
- ✓ Todos os scripts referenciados e configurados
- ✓ Materiais básicos atribuídos

**Não precisa fazer nada manual!**

---

## 📖 Opção 2: Manual

Se preferir montar manualmente (para aprender):

Siga o arquivo **`SETUP_CENA.md`** que tem 11 passos detalhados.

Tempo estimado: 15-20 minutos

---

## 🚀 Após o Setup Automático

1. **Play** (pressione Play ou Ctrl+P)
2. Teste:
   - **WASD** = movimento
   - **Espaço** = peido
   - **ESC** = sair

3. Valide com **`TESTES_ETAPA1.md`** (20 testes rápidos)

---

## Troubleshooting

**"Menu 'Farty Farm' não aparece"**
→ Aguarde Unity compilar (barra de progresso)
→ Ou: Assets → Reimport All

**"Erro ao salvar cena"**
→ Crie a pasta manualmente: Assets → Create → Folder → "Scenes"
→ Tente de novo

**"GameObject não aparece"**
→ Console > Clear
→ Tente de novo

---

## Estrutura Final

Após rodar o script, seu projeto terá:

```
Assets/
├── Scenes/
│   └── Arena.unity              ← Cena principal (pronta pra jogar)
├── Scripts/
│   ├── Characters/BoiController.cs
│   ├── Camera/TopDownCamera.cs
│   └── Game/GameManager.cs
└── Editor/
    └── SceneSetup.cs            ← Script que montou tudo
```

---

**Próximas Etapas:**
- Etapa 2: Moedas, argolas, pontuação
- Etapa 3: Multiplayer LAN

Aproveite o jogo! 🎮
