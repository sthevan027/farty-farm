# Testes — Etapa 1: Protótipo de Movimentação

Lista de testes reproduzíveis para validar que a Etapa 1 está funcionando corretamente.

## Testes Básicos de Movimento

### T1.1 — Aceleração Suave
**Procedimento:**
1. Abra o jogo (Play)
2. Pressione **W** por 2 segundos
3. Observe o boi

**Esperado:**
- Boi acelera gradualmente (não salta para velocidade máxima)
- Velocidade indicada no canto aumenta de forma suave
- Console não mostra erros

**Falha se:**
- Boi pula instantaneamente para velocidade alta
- Boi não se move
- Erro no Console

---

### T1.2 — Inércia e Fricção
**Procedimento:**
1. Abra o jogo
2. Pressione **W** por 2 segundos, depois solte
3. Observe quanto tempo leva para parar

**Esperado:**
- Boi continua movendo após soltar W
- Para gradualmente (não instantaneamente) em ~2-3 segundos
- Velocidade no display decresce suavemente

**Falha se:**
- Boi para imediatamente
- Boi desliza infinitamente

---

### T1.3 — Limite de Velocidade
**Procedimento:**
1. Abra o jogo
2. Pressione **W** continuamente por 10 segundos
3. Observe a velocidade máxima

**Esperado:**
- Velocidade máxima atinge ~30 (ou o valor em Max Speed)
- Não aumenta mais mesmo com W pressionado

**Falha se:**
- Velocidade continua crescendo indefinidamente

---

### T1.4 — Movimento Multi-Direção
**Procedimento:**
1. Abra o jogo
2. Teste cada direção: **W**, **A**, **S**, **D** individualmente
3. Teste combinações: **WA**, **WD**, **AS**, **AD**

**Esperado:**
- Cada direção funciona isoladamente
- Combinações funcionam (diagonal)
- Boi rotaciona para a direção correta

**Falha se:**
- Alguma direção não funciona
- Boi não rotaciona corretamente
- Movimento diagonal é errado

---

## Testes do Sistema de Gás

### T2.1 — Peido Básico
**Procedimento:**
1. Abra o jogo com gás cheio (100%)
2. Pressione **Espaço** uma vez
3. Observe o boi e o gás

**Esperado:**
- Boi é impulsionado para trás (peido sai para frente)
- Gás decresce de 100% para 80% (perde 20%)
- Console mostra: "PRRRRR! Gás restante: 80/100"
- Partículas de peido são visíveis

**Falha se:**
- Boi não se move
- Gás não decresce
- Nenhuma mensagem no Console

---

### T2.2 — Sem Gás
**Procedimento:**
1. Abra o jogo
2. Pressione **Espaço** 5+ vezes rapidamente (para gastar todo o gás)
3. Tente pressionar **Espaço** mais

**Esperado:**
- Primeiros peidos funcionam
- Quando gás chega a ~0%, o 6º peido é bloqueado
- Console mostra: "Sem gás!"
- Boi não se impulsiona

**Falha se:**
- Permite peido sem gás
- Não mostra mensagem "Sem gás"

---

### T2.3 — Regeneração de Gás
**Procedimento:**
1. Abra o jogo
2. Gaste todo o gás (Espaço 5+ vezes)
3. Espere 10 segundos
4. Observe o gás regenerando

**Esperado:**
- Gás volta a 100% em ~6-7 segundos (100 / 15 rate)
- Depois é possível usar peido novamente
- Regeneração é contínua e suave

**Falha se:**
- Gás não regenera
- Regeneração é instantânea
- Regeneração é muito lenta (>20 segundos)

---

### T2.4 — Cooldown entre Peidos
**Procedimento:**
1. Abra o jogo
2. Pressione **Espaço**, solte imediatamente, pressione de novo imediatamente
3. Veja o tempo entre peidos

**Esperado:**
- Segundo peido não funciona imediatamente
- Precisa esperar ~0.3 segundos
- Console mostra apenas um "PRRRRR!" por vez

**Falha se:**
- Dois peidos são emitidos em sequência muito rápida
- Console mostra dois "PRRRRR!" ao mesmo tempo

---

## Testes de Câmera

### T3.1 — Câmera Acompanha
**Procedimento:**
1. Abra o jogo
2. Mova o boi com **W** para frente por alguns metros
3. Observe a câmera

**Esperado:**
- Câmera acompanha suavemente o boi
- Boi permanece aproximadamente no centro da tela
- Câmera não "pula" bruscamente

**Falha se:**
- Câmera não acompanha
- Câmera fica fixa
- Câmera salta bruscamente

---

### T3.2 — Perspectiva Correta
**Procedimento:**
1. Abra o jogo
2. Mova o boi em diferentes direções
3. Observe o ângulo da câmera

**Esperado:**
- Câmera vê de cima + diagonal (terceira pessoa perspectiva)
- Profundidade é perceptível
- Você consegue ver frente, lados e um pouco atrás do boi

**Falha se:**
- Câmera é ortográfica (sem perspectiva)
- Câmera fica muito perto ou muito longe
- Ângulo não é apropriado

---

### T3.3 — Sem Clipping
**Procedimento:**
1. Abra o jogo
2. Mova o boi para os cantos da arena
3. Observe se a câmera atravessa paredes

**Esperado:**
- Câmera não atravessa paredes
- Mantém distância do boi mesmo nos cantos

**Falha se:**
- Câmera passa através de paredes
- Câmera entra dentro do boi

---

## Testes de Físicas e Colisões

### T4.1 — Colisão com Paredes
**Procedimento:**
1. Abra o jogo
2. Acelere em direção a uma parede (W)
3. Observe o que acontece

**Esperado:**
- Boi bate na parede e para
- Não atravessa a parede
- Não fica preso

**Falha se:**
- Boi atravessa a parede
- Boi fica preso (precisa de impulso para sair)
- Boi é ejetado com força exagerada

---

### T4.2 — Colisão com Chão
**Procedimento:**
1. Abra o jogo
2. Observe o boi em repouso

**Esperado:**
- Boi repousa sobre o chão (não cai infinitamente)
- Y-position é ~0.5 (levemente acima do chão)
- Nenhum "bobbing" ou vibração

**Falha se:**
- Boi cai infinitamente
- Boi vibra ou salta
- Boi afunda no chão

---

### T4.3 — Impulso dos Peidos Não Levanta o Boi
**Procedimento:**
1. Abra o jogo
2. Pressione **Espaço** enquanto em movimento
3. Observe a trajetória (deve ser horizontal)

**Esperado:**
- Peido impulsiona horizontalmente (não para cima)
- Boi não salta
- Movimento permanece no plano do chão

**Falha se:**
- Boi salta para o ar
- Peido tem componente Y significativo

---

## Testes de UI

### T5.1 — Display de Gás
**Procedimento:**
1. Abra o jogo
2. Observe o texto "Gás: 100%" no canto superior esquerdo
3. Use peidos e observe mudanças

**Esperado:**
- Texto é legível (branco sobre fundo escuro)
- Porcentagem atualiza em tempo real
- Valores são precisos (100%, 80%, etc)

**Falha se:**
- Texto não aparece
- Texto é ilegível
- Valores não atualizam

---

### T5.2 — Display de Velocidade
**Procedimento:**
1. Abra o jogo
2. Observe o texto "Vel: X" no canto superior esquerdo
3. Mova o boi e observe mudanças

**Esperado:**
- Texto mostra velocidade em tempo real
- Aumenta conforme acelera
- Decresce conforme desacelera
- Vai a 0 quando para

**Falha se:**
- Texto não aparece
- Valores não atualizam
- Valores são absurdos (negativos, muito grandes)

---

### T5.3 — Instruções Visíveis
**Procedimento:**
1. Abra o jogo
2. Procure pelo texto de instruções

**Esperado:**
- Vê instruções: "WASD = Mover | Espaço = Peido"
- Texto é legível
- Localização não obstrui a ação

**Falha se:**
- Instruções não aparecem
- Instruções cobrem a arena

---

## Testes de Console

### T6.1 — Sem Erros
**Procedimento:**
1. Abra o jogo
2. Abra Console (**Ctrl+Shift+C** ou **Window > General > Console**)
3. Tente todas as ações por ~30 segundos
4. Observe o Console

**Esperado:**
- Apenas mensagens "PRRRRR!" e "Sem gás!" aparecem
- Nenhuma mensagem de erro (vermelha)
- Nenhuma exceção

**Falha se:**
- Erros aparecem no Console
- Exceções NullReferenceException, etc

---

### T6.2 — Mensagens de Debug
**Procedimento:**
1. Use peido enquanto observa o Console
2. Observe as mensagens

**Esperado:**
- Console mostra: "PRRRRR! Gás restante: XX/100"
- Quando sem gás: "Sem gás!"

**Falha se:**
- Mensagens não aparecem
- Mensagens estão confusas ou sem contexto

---

## Teste de Estabilidade

### T7.1 — Múltiplas Rodadas
**Procedimento:**
1. Abra o jogo
2. Jogue por 5 minutos
3. Faça movimento caótico (WASD aleatório + Espaço frequente)
4. Observe estabilidade

**Esperado:**
- Jogo não trava
- Nenhum lag ou stuttering
- Boi responde consistentemente
- Console não fica vermelho com erros

**Falha se:**
- Jogo trava ou congela
- Performance degrada com o tempo
- Comportamento inesperado aparece

---

## Checklist Final

- [ ] T1.1 — Aceleração Suave ✓
- [ ] T1.2 — Inércia e Fricção ✓
- [ ] T1.3 — Limite de Velocidade ✓
- [ ] T1.4 — Movimento Multi-Direção ✓
- [ ] T2.1 — Peido Básico ✓
- [ ] T2.2 — Sem Gás ✓
- [ ] T2.3 — Regeneração de Gás ✓
- [ ] T2.4 — Cooldown entre Peidos ✓
- [ ] T3.1 — Câmera Acompanha ✓
- [ ] T3.2 — Perspectiva Correta ✓
- [ ] T3.3 — Sem Clipping ✓
- [ ] T4.1 — Colisão com Paredes ✓
- [ ] T4.2 — Colisão com Chão ✓
- [ ] T4.3 — Impulso Não Levanta o Boi ✓
- [ ] T5.1 — Display de Gás ✓
- [ ] T5.2 — Display de Velocidade ✓
- [ ] T5.3 — Instruções Visíveis ✓
- [ ] T6.1 — Sem Erros ✓
- [ ] T6.2 — Mensagens de Debug ✓
- [ ] T7.1 — Múltiplas Rodadas ✓

**Se todos passarem: Etapa 1 está validada!** 🎉

---

## Bugs Encontrados

Nenhum relatado ainda.

**Data de Testes:** 2026-10-18
