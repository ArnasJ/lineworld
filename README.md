# Line World console game simulation

```

  [Gold] P1: 15 / 100 | P2: 30 / 100
  
  ### . . . . ] . . . } . . ) ) . ] . ] . . [ . [ . { . ( . ( . ###
  100         6       4     4 6   8   8     9   9   7   5   5   100 <- HP indicators
    ^         ^       ^     ^ ^   ^   ^     ^   ^   ^   ^   ^   ^
     \         \_______\_____\_\___\___\     \___\___\___\___\   \__ P2 castle
      \__ Player 1 (P1) castle          \__ P1 Units          \__ P2 Units

```
This is a console based mini-game where two players fight in a one-dimensional world (a line), each aiming to destroy each others castle to achieve victory.



### Game World
The world for the game is a simple line of 30 slots.  
Game advances in turns.

### Castle
Each player has a castle at each end of the line.
- Each player has a castle at each end of the line.
- A castle has 100 health points (HP).
- The castle is not considered a part of the word, thus it does not take any space in the world.
- However if you are able to attack past the end of the field (indexes -1 or 30, respectively), then you are able to attach the castle.

### Resources
- Your castle stores a maximum of 100 gold.
- You start with 20 gold.
- Each turn your castle generates 10 gold.
- Each killed enemy unit provides you with 20% of its cost, rounded down. So, for example, if you kill a unit that has a cost of 10, you would get 2 gold.

### Units
There are several types of units in Line World. They all take 1 cell in the world.
Units never have more than 9 HP (so that HP fits into a single digit).
<details>
  <summary>Warrior</summary>
  
  **Indication:** `]` (player 1) or `[` (player 2)  
  **Cost:** 15  
  **Damage:** 4  
  **HP:** 9  
  **Range:** 1 - can only attack units that are in an adjacent cell.
</details>
<details>
  <summary>Archer</summary>

  **Indication:** `]` (player 1) or `[` (player 2)  
  **Cost:** 20  
  **Damage:** 2  
  **HP:** 5  
  **Range:** 4 - can attack players up to 4 cells from itself.
</details>
<details>
  <summary>Cleric</summary>

  **Indication:** `)` (player 1) or `(` (player 2)  
  **Cost:** 30  
  **Damage:** 1 (to enemies) or can heal a friendly unit for 3 (every 3 turns)  
  **HP:** 3  
  **Range:** 6
</details>

- Spawning units is instantaneous (does not take multiple turns).
- You can only spawn one unit per turn.
- Units spawn at the end of the field (indexes 0 and 29, respectively).
- If that space is blocked, when a unit is spawned, every blocking unit is pushed one cell forwards.
    
    ```
    Before archer is spawned:
    ### ] ] . ] . .
    
    Archer spawned:
    ### } ] ] ] . .
    ```
- Units move at a speed of 1 cell per turn.
- Units that are closer to the enemy castle act (move/attack/heal) first.
- Unit can only move or attack per turn, not both.
- Units only move forwards toward enemy castle.
- If it is possible for unit to heal, it has to. The target is unit with the lowest HP.
- Else if it is possible for unit to attack, it has to. The target is unit with the lowest HP.
- Else the unit moves forwards.
- If the unit can not move forward because it is blocked by a friendly unit, it can swap places with it if the blocking unit has lower HP.

### Goal
Write an AI, which gives one of these orders each turn. That AI will play as both players.
- Spawn Warrior / Archer / Cleric.
- Do nothing.
- The simplest AI just picks a valid action at random. If you want, you can implement something more clever.
