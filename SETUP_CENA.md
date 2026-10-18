# Guia: Montar a Cena Principal no Unity

Após abrir o projeto no Unity Editor, siga este guia para montar a cena da Etapa 1.

## 1. Criar a Cena Principal

1. Na aba **Project**, navegue até `Assets/Scenes/`
2. Clique direito → **Create** → **Scene**
3. Nome: `Arena`
4. Double-click para abrir a cena

A cena será criada com um GameObject vazio chamado "Arena" (que você pode renomear para algo melhor depois).

## 2. Configurar o Chão (Arena)

1. Clique direito na Hierarchy → **3D Object** → **Plane**
2. Renomeie para `Ground`
3. No Inspector:
   - **Position**: (0, 0, 0)
   - **Scale**: (10, 1, 10) — arena 20x20 unidades
4. Adicione um **Box Collider** (para física)
5. Crie um material verde simples:
   - Assets → Create → Material
   - Nome: `Mat_Grass`
   - Color: Verde (0.3, 0.8, 0.3)
   - Arraste para o Ground

## 3. Criar as Paredes/Limites

1. Clique direito → **3D Object** → **Cube**
2. Renomeie para `Wall_Front`
3. Configure:
   - **Position**: (0, 0.5, 10)
   - **Scale**: (20, 1, 1)
   - Remova o **Box Collider** padrão de colisão se quiser (ou mantenha para bloqueio)

Repita para as outras 3 paredes:
- `Wall_Back`: Position (0, 0.5, -10)
- `Wall_Left`: Position (-10, 0.5, 0), Scale (1, 1, 20)
- `Wall_Right`: Position (10, 0.5, 0), Scale (1, 1, 20)

Crie um material cinza para as paredes:
- Material: `Mat_Wall`
- Color: Cinza (0.5, 0.5, 0.5)

## 4. Criar o Boi (Personagem Principal)

### 4.1 GameObject principal
1. Clique direito → **Create Empty**
2. Renomeie para `Boi`
3. Position: (0, 0.5, 0)
4. Adicione um **Rigidbody**:
   - **Body Type**: Dynamic
   - **Mass**: 1
   - **Drag**: 0
   - **Angular Drag**: 0.1
   - **Gravity**: Ativado (checked)
   - **Freeze Rotation**: Marque X, Z (só rotaciona em Y)

### 4.2 Criar o corpo visual
1. Dentro do Boi, crie: **3D Object** → **Cube**
2. Renomeie para `Body`
3. Position: (0, 0, 0)
4. Scale: (1, 0.6, 1)
5. Material: Crie `Mat_Boi_Body` (cor alaranjada: 1.0, 0.6, 0.2)

### 4.3 Criar a cabeça
1. Dentro do Boi, crie: **3D Object** → **Sphere**
2. Renomeie para `Head`
3. Position: (0, 0.7, 0)
4. Scale: (0.8, 0.8, 0.8)
5. Material: `Mat_Boi_Head` (alaranjado um pouco mais escuro)

### 4.4 Criar os olhos
1. Dentro de Head, crie: **3D Object** → **Sphere**
2. Renomeie para `EyeLeft`
3. Position: (-0.25, 0.2, 0.35)
4. Scale: (0.2, 0.2, 0.15)
5. Material: `Mat_Eye_White` (branco: 1, 1, 1)

6. Copie para `EyeRight`:
   - Position: (0.25, 0.2, 0.35)

7. Dentro de cada olho, crie uma pequena Sphere para a pupila:
   - `EyeLeft > Pupil`: Position (0, 0, 0.15), Scale (0.12, 0.12, 0.08)
   - Material: `Mat_Eye_Pupil` (preto: 0, 0, 0)

**Importante**: Ao selecionar **EyeLeft** e **EyeRight** no BoiController, o script as escalará quando o boi usar peido.

## 5. Adicionar Collider ao Boi

1. Selecione o Boi (GameObject pai, não Body/Head)
2. Add Component → **Capsule Collider**
3. Configure:
   - **Height**: 1.6
   - **Radius**: 0.5
   - **Center**: (0, 0.3, 0)

Remova os Box Colliders de Body e Head para evitar colisões duplas:
- Selecione Body → Remove Component (Box Collider)
- Selecione Head → Remove Component (Box Collider)

## 6. Adicionar Partículas de Peido

1. Dentro do Boi, crie: **Effects** → **Particle System**
2. Renomeie para `FartParticles`
3. Position: (0, 0, -0.5) — saindo da traseira
4. Configure (no Inspector):
   - **Start Lifetime**: 1 segundo
   - **Start Speed**: 5
   - **Start Size**: 0.3
   - **Emission**: 10 particles/sec
   - **Renderer**: Material = `Mat_Particle` (cria um cinza translúcido)

Desative o auto-play:
- Emission → Desabilite ou configure para não iniciar automaticamente

Deixe "Play on Awake" desligado (unchecked).

## 7. Scripts e Referências

### No GameObject Boi:
1. Add Component → **BoiController**
2. No Inspector, configure as referências:
   - **Rb** (Rigidbody): Arraste o Rigidbody do Boi
   - **Eye Left Bone**: Arraste `EyeLeft`
   - **Eye Right Bone**: Arraste `EyeRight`
   - **Fart Particles**: Arraste `FartParticles`

Deixe os parâmetros no padrão (Acceleration: 50, Max Speed: 30, etc.) por enquanto.

## 8. Câmera

### Opção A: Câmera Padrão + Script
1. Selecione **Main Camera** (deve existir)
2. Add Component → **TopDownCamera**
3. Configure:
   - **Target**: Arraste o Boi
   - **Distance**: 15
   - **Height**: 10
   - **Angle**: 45
   - **Smooth Damp**: 0.3

### Opção B: Criar Nova Câmera
1. Clique direito → **3D Object** → **Camera**
2. Renomeie para `MainCamera`
3. Add Component → **TopDownCamera** (configure como acima)

## 9. GameManager (UI)

1. Clique direito na Hierarchy → **Create Empty**
2. Renomeie para `GameManager`
3. Add Component → **GameManager**
4. No Inspector:
   - **Boi Controller**: Arraste o Boi
   - **Canvas**: Deixe vazio (script cria automaticamente)

Nota: Se quiser usar um Canvas existente, crie um:
1. Clique direito → **UI** → **Canvas**
2. Arraste para o GameManager
3. Configure no Inspector do GameManager

## 10. Testar a Cena

1. **File** → **Save Scene** (Ctrl+S)
   - Ou acima na Hierarchy, com a cena aberta, Ctrl+S
2. Pressione **Play** (ou Ctrl+P)
3. Tente:
   - **WASD** — deve se mover
   - **Espaço** — deve emitir peido e se impulsionar
   - Veja a barra de gás no canto superior esquerdo
   - A câmera deve acompanhar

Se houver erros:
- Abra **Window** → **General** → **Console** (Ctrl+Shift+C)
- Leia as mensagens de erro (vermelho)
- Volte ao passo anterior e corrija

## 11. Salvar a Cena

**File** → **Save Scene** ou **Ctrl+S**

Parabéns! Etapa 1 está funcionando. 🎉

---

**Próximos passos (Etapa 2):**
- Adicionar moedas coletáveis
- Adicionar argolas
- Sistema de fogo e incêndios
- Vaquinha (IA simples)
