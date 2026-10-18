# Farty Farm — Etapa 1: Protótipo de Movimentação

Jogo 3D multiplayer LAN com mecânica de "peidos direcionais" como impulsos físicos.

## Estado Atual

**Etapa 1 — Prototipo de Movimentação**

Arquivos criados:
- ✅ Estrutura de pastas do projeto
- ✅ `BoiController.cs` — controle do boi com Rigidbody, movimento WASD, peidos com Espaço
- ✅ `TopDownCamera.cs` — câmera terceira pessoa perspectiva
- ✅ `GameManager.cs` — UI básica com gás, velocidade
- ⏳ Cenas Unity (precisa montá-las no editor)

## Como Abrir o Projeto no Unity

### Opção 1: Usando Unity Hub (Recomendado)

1. Abra **Unity Hub**
2. Clique em **"Add"** (ou **"Open"**)
3. Navegue até `/home/sthevandev/Projetos/farty-farm`
4. Selecione a pasta e clique em **"Open**
5. Unity 2022 LTS será detectado e usado automaticamente
6. Aguarde o projeto abrir (primeira vez leva mais tempo)

### Opção 2: Linha de Comando

```bash
/path/to/Unity/2022.3.x/Editor/Unity -projectPath /home/sthevandev/Projetos/farty-farm
```

(Substitua `2022.3.x` pela versão exata instalada)

## Estrutura do Projeto

```
farty-farm/
├── Assets/
│   ├── Scenes/           # Cenas Unity (.unity)
│   ├── Scripts/
│   │   ├── Characters/   # BoiController.cs
│   │   ├── Mechanics/    # Sistemas de mecânica
│   │   ├── Camera/       # TopDownCamera.cs
│   │   ├── Game/         # GameManager.cs
│   │   ├── UI/
│   │   └── Network/
│   ├── Prefabs/          # Modelos reutilizáveis
│   ├── Materials/        # Materiais
│   ├── Audio/            # Sons
│   └── Particles/        # Efeitos de partículas
├── ProjectSettings/      # Configurações do Unity
├── Packages/             # Dependências (manifest.json)
└── README.md
```

## Próximos Passos: Montar a Cena Inicial

Após abrir o projeto no Unity:

1. **Criar cena principal** (`Assets/Scenes/Arena.unity`)
2. **Hierarquia básica:**
   - Empty GameObject → `Boi` (com Rigidbody + BoiController)
   - Empty GameObject → `Arena` (com MeshCollider e visual)
   - Empty GameObject → `Main Camera` (com TopDownCamera)
   - Empty GameObject → `GameManager` (com GameManager.cs)

3. **Configurar o Boi:**
   - Add Rigidbody (Rb dynamics)
   - Add Box/Capsule Collider
   - Add BoiController script
   - Criar meshes simples para cabeça/corpo (ou usar primitivos do Unity)

4. **Criar modelo 3D simples do boi** (pode ser cube/sphere por enquanto):
   - Corpo: Cube escalonado
   - Cabeça: Sphere
   - Olhos: Duas pequenas Spheres (tags: EyeLeft, EyeRight)

5. **Configurar câmera:**
   - Add TopDownCamera script
   - Atribuir `target` = Transform do Boi

6. **Arena:**
   - Criar Plane para chão
   - Add MeshCollider
   - Criar paredes/limites com Cubes

7. **Testar:**
   - Play
   - WASD = movimento
   - Espaço = peido
   - Veja o gás descarregar e regenerar

## Testes para Validar Etapa 1

- [ ] Boi move com WASD (aceleração suave, não instantânea)
- [ ] Boi tem inércia — não para imediatamente ao soltar WASD
- [ ] Espaço gera impulso visível
- [ ] Gás descarrega ao usar peido
- [ ] Gás regenera gradualmente
- [ ] Boi não pode usar peido sem gás
- [ ] Câmera acompanha suavemente o boi
- [ ] Boi roda para a direção do movimento
- [ ] Sem erros no Console

## Parâmetros Ajustáveis (BoiController.cs)

No Inspector do Unity, ao selecionar o Boi:

- **Acceleration** — força de movimento (padrão: 50)
- **Max Speed** — velocidade máxima (padrão: 30)
- **Friction** — desaceleração (padrão: 0.95)
- **Fart Force** — força do impulso (padrão: 20)
- **Gas Capacity** — capacidade total (padrão: 100)
- **Gas Per Fart** — custo por peido (padrão: 20)
- **Gas Regen Rate** — velocidade de regeneração (padrão: 15)
- **Fart Cooldown** — intervalo mínimo entre peidos (padrão: 0.3s)

Ajuste esses valores se o jogo não se sentir divertido.

## Problemas Comuns

**"Project not found" no Unity Hub**
→ Certifique-se de que a pasta contém `ProjectSettings/` e `Assets/`

**Scripts aparecem com erro (?) vermelho**
→ Clique em Assets > Reimport All
→ Ou aguarde o Unity recompilar automaticamente

**Boi não se move**
→ Verifique se o BoiController está attached ao GameObject do boi
→ Verifique se o Rigidbody está configurado (Body Type: Dynamic)

**Câmera não acompanha**
→ Verifique se TopDownCamera está no GameObject Camera
→ Verifique se o `target` está atribuído (deve apontar para o Transform do boi)

## Próxima Etapa

Após validar Etapa 1, iremos adicionar:
- Moedas coletáveis
- Argolas
- Sistema de fogo
- Vaquinha (IA simples)
- Pontuação

---

**Criado para:** Sthevan  
**Engine:** Unity 2022 LTS  
**Linguagem:** C#  
**Data:** 2026-10-18
