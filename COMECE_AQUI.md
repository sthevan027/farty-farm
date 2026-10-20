# 🎮 Farty Farm — Comece Aqui

Jogo 3D multiplayer LAN em Unity 6 — veja **NOVIDADES.md** para a versão atual

---

## ✅ O Que Já Está Pronto

**100% da Etapa 1 montada:**
- ✓ Scripts C# (Boi, Câmera, GameManager)
- ✓ Editor Script para montar a cena automaticamente
- ✓ Documentação completa
- ✓ Testes validação

---

## 🚀 Comece em 2 Passos

### Passo 1: Abrir no Unity

1. Abra **Unity Hub**
2. Clique **"Open"**
3. Navegue até: `/home/sthevandev/Projetos/farty-farm`
4. Selecione a pasta e abra (usará **Unity 2022 LTS**)

### Passo 2: Montar a Cena (1 clique)

1. No menu superior: **Farty Farm** → **Setup Arena Scene**
2. Aguarde ~5 segundos
3. Console mostrará: ✓ "Arena scene criada com sucesso"

**Pronto! Sua cena está 100% pronta.**

---

## 🎮 Testar

1. Pressione **Play** (Ctrl+P)
2. Use:
   - **WASD** — Mover
   - **Espaço (segurar)** — Peido contínuo (boi) / Leite (vaca)
   - **ESC** — Sair

3. Veja no canto superior:
   - **Gás: XX%** — sua barra de combustível
   - **Vel: XX** — velocidade atual

---

## 📚 Documentação

| Arquivo | O Que É |
|---------|---------|
| **SETUP_AUTOMATICO.md** | Como montar a cena (o que você vai fazer) |
| **SETUP_CENA.md** | Guia manual (se preferir montar à mão) |
| **TESTES_ETAPA1.md** | 20 testes para validar que tudo funciona |
| **README.md** | Visão geral técnica do projeto |

---

## 🔧 Parâmetros Ajustáveis

Após montar, selecione o GameObject "Boi" e veja no Inspector:

- **Acceleration** (padrão: 50) — força de movimento
- **Max Speed** (padrão: 30) — velocidade máxima
- **Fart Force** (padrão: 20) — força do impulso do peido
- **Gas Capacity** (padrão: 100) — capacidade total
- **Gas Per Fart** (padrão: 20) — quanto cada peido consome
- **Gas Regen Rate** (padrão: 15) — velocidade de regeneração

**Se não parecer divertido, ajuste esses valores no Inspector e teste novamente.**

---

## 📋 Checklist Rápido

Após montar e testar, verifique:

- [ ] Boi move com WASD (suave, não instantâneo)
- [ ] Espaço faz peido com impulso visível
- [ ] Gás descarrega e regenera
- [ ] Câmera acompanha suavemente
- [ ] Sem erros vermelhos no Console

Se tudo passou: **Etapa 1 está validada!** ✓

---

## 🔜 Próximas Etapas

Quando quiser continuar:

**Etapa 2 — Mecânicas da Arena**
- Moedas coletáveis
- Argolas suspensas
- Obstáculos
- Pontuação
- Vaquinha (IA simples)

**Etapa 3 — Multiplayer LAN**
- Conectar dois computadores
- Sincronizar jogadores
- Pontuação competitiva

---

## ❓ Problemas?

**Unity não detecta a pasta?**
→ Certifique-se de que há `ProjectSettings/` e `Assets/` na pasta

**Menu "Farty Farm" não aparece?**
→ Assets → Reimport All
→ Aguarde compilação

**Erro ao rodar SceneSetup?**
→ Abra Console (Ctrl+Shift+C) e leia o erro
→ Se der erro de salvamento, crie `Assets/Scenes/` manualmente

---

## 📞 Suporte Técnico

Tudo está documentado. Se precisar de ajuda:

1. Leia **SETUP_AUTOMATICO.md** (a coisa mais simples)
2. Verifique **TESTES_ETAPA1.md** para entender o que deveria acontecer
3. Abra Console (Ctrl+Shift+C) e procure por mensagens vermelhas

---

**Divirta-se! 🎮**

Criado: 2026-10-18  
Engine: Unity 2022 LTS  
Linguagem: C#  
Etapa: 1/6
