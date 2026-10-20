# Farty Farm — Versão 4

## Como atualizar
Substitua o projeto (ou a pasta `Assets/`) e abra no Unity 6. A cena é recriada automaticamente
(SceneVersion = 4). **Na primeira vez demora ~30 s**: gera texturas e "esculpe" os animais
(barra de progresso). Para refazer à mão: menu **Farty Farm → Setup Arena Scene**.

## 1. Movimentação de gelo (boi e vaca)
**Causa provável do "boi sem impulso":** o peido aplicava `AddForce` e, no mesmo passo de física,
o script sobrescrevia a velocidade, então a força podia ser engolida. Agora a velocidade é somada
direto — sem depender de `AddForce`.

**Boi (BoiController)** — segure Espaço:
- tranco inicial (`startKick`) + aceleração forte (`fartThrust` = 13) até `maxSpeed` = 11;
- **atrito quase zero** (`iceDrag` = 0,22): ele continua deslizando vários segundos depois de soltar;
- o corpo gira por **mola com pouco amortecimento** (`turnDamping`): passa do ponto e balança;
- enquanto peida a direção **treme** (`fartWobble`), então o caminho fica torto e maluco;
- bate em árvore/parede e **ricocheteia** (material `Bouncy`, bounciness 0,6) e **rodopia** (`bumpSpin`);
- a câmera se afasta com a velocidade.

**Vaca (VacaController):** acelera devagar (`acceleration`), troca de direção derrapando e
desliza quando solta as teclas (`iceDrag`). As rodinhas dos patins giram conforme a velocidade.

Quer mais/menos maluco? `turnDamping` (menor = mais balanço), `iceDrag` (menor = mais gelo),
`fartWobble`, `fartThrust`, `maxSpeed`.

## 2. Animais refeitos
Corpo, cabeça, chifres, orelhas, pernas, coleira, sino e argola do nariz agora são **uma malha
orgânica lisa** (formas fundidas com união suave, AO baked nos vértices), não mais esferas empilhadas.
- Shader próprio `FartyFarm/Animal` (`Assets/Shaders`): cor por região no vértice + padrão de
  pelagem triplanar (vaca malhada, boi peludo) + luz de contorno suave.
- Olhos grandes com brilho (ainda esticam ao peidar/soltar leite), úbere com 4 tetas separado
  (pulsa ao ordenhar), patins modelados com rodinhas que giram, rabo animado.
- As formas estão em `Assets/Editor/AnimalShapes.cs`; as malhas são salvas em `Assets/Meshes`.

## 3. Texturas e cenário
- Folhas com **normal map** e deformação orgânica; casca com mais veios; grama com camada de
  detalhe em grande escala (some a repetição); cerca de **madeira** nas paredes.
- Céu procedural, névoa suave, luz solar quente e luz ambiente em 3 tons.

## 4. Setas estilo GTA V (coletar moedas)
- 12 **moedas** por onda, cada uma com **seta 3D** amarela com contorno, girando e flutuando
  (cresce com a distância para continuar visível). A última coletada gera uma nova onda.
- **Seta na borda da tela** apontando para a moeda mais próxima quando ela está fora de vista,
  com a distância em metros.
- **Radar circular** (canto inferior esquerdo): moedas amarelas (a mais próxima pulsa), incêndios
  vermelhos; blips fora do alcance ficam presos na borda; seu personagem é o triângulo central.
- Cada moeda devolve 25 de gás/leite (`gasBonus` em `CollectibleManager`).
- Só o personagem controlado pelo jogador coleta.

---

# Versão 3 (anterior)

## Como atualizar
1. Substitua a pasta do projeto por esta (ou copie `Assets/`).
2. Abra no Unity 6. A cena é **recriada automaticamente** (SceneVersion = 3) e as
   texturas são geradas por código — a primeira vez leva alguns segundos (barra de progresso).
3. Se quiser refazer manualmente: menu **Farty Farm → Setup Arena Scene**.

## Visual
- **Árvores**: tronco afunilado com raiz alargada, textura de casca + normal map; copa feita de
  vários tufos amassados por ruído, com textura de folhas. 3 formatos de copa × 3 tons de verde,
  rotação aleatória por árvore. Quando queimam, copa e tronco ficam carbonizados.
- **Vaca**: pelagem branca malhada (preto/marrom) + 4 tetas visíveis no úbere.
- **Boi**: pelagem marrom com variação de tom e fios de pelo.
- **Chão**: textura de grama.
- Texturas ficam em `Assets/Textures`, malhas em `Assets/Meshes`, materiais em `Assets/Materials`.

## Rabinho animado (`TailAnimator.cs`)
Rabo de 3 juntas com tufo. Parado: balança devagar. Andando: balança mais.
- **Boi**: enquanto peida, o rabo **levanta e chacoalha**.
- **Vaca**: ao soltar leite, o rabo agita.

## Vaca soltando leite (`CowMilkAnimator.cs`)
Ao apertar **Espaço**: a vaca **se levanta nas patas de trás** (com um pequeno "quique"),
o leite **sai pelas 4 tetas** (o úbere pulsa), e ela volta ao chão. Dura ~1,4 s.
- O leite só apaga o fogo enquanto está saindo; um jato derruba o fogo por completo
  (`milkPower` = 130, o fogo tem 100 de vida).
- Ela anda devagar (30%) durante a animação. Ajustável no Inspector.

## Peido de segurar (`BoiController.cs`)
**Segure Espaço** = peido contínuo; solte = para.
- Empurra o boi com aceleração contínua (`fartThrust`) + um chutinho inicial (`startKick`).
- Gasta gás por segundo (`gasDrainPerSecond`, 30/s ≈ 3,3 s de peido do tanque cheio).
- O gás só volta a regenerar 0,4 s depois de soltar (`gasRegenDelay`).
- Acabou o gás? Precisa soltar e apertar de novo; só começa com pelo menos 10 de gás (`minGasToStart`).
- Chama + nuvem de gás esverdeada contínuas, e incendeia árvores atrás dele enquanto segura.
- O boi controlado pelo PC também peida em "rajadas" de 0,5–1,2 s.

## Ajustes rápidos (Inspector)
| Quer... | Mexa em |
|---|---|
| peido mais forte/fraco | `BoiController.fartThrust`, `maxSpeed` |
| peido durar mais | `BoiController.gasDrainPerSecond` (menor = dura mais) |
| vaca levantar mais/menos | `CowMilkAnimator.rearAngle` |
| jato de leite mais longo | `CowMilkAnimator.sprayTime`, velocidade em `CreateMilkSpray` (SceneSetup) |
| rabo mais animado | `TailAnimator.wagAmplitude`, `wagFrequency`, `liftAngle` |
