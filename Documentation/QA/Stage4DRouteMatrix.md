# Stage 4D ending route matrix

Generated from `GuanduDialogueData.asset`; use the option text and next node as the replay starting point.

| Anchor | Option count | Replay options |
| --- | ---: | --- |
| `1001` | 3 | `100201` 连夜赶造发石车，以牙还牙<br>`100301` 令将士挖掘地道，奇袭袁营<br>`1004` 佯装败退，设下埋伏 |
| `2001` | 2 | `200201` 采纳文若之谏，坚守官渡，等待战机<br>`200301` 粮草乃根本，不如暂回许都，再图后举 |
| `3001` | 3 | `300201` 坦诚相告，推心置腹<br>`300301` 虚与委蛇，试探虚实<br>`300401` 疑心生暗鬼，推出去斩首 |
| `4001` | 2 | `400201` “孤当亲自前往，以振军威！”<br>`400301` 令徐晃、张辽等率精兵前往 |
| `5001` | 3 | `500201` 不予理会，全力攻占乌巢<br>`500301` 派曹洪分兵回守大营<br>`500401` 趁其大营空虚，直取袁绍 |

## Ending checkpoints

| Checkpoint | Expected route or gate |
| --- | --- |
| IF1 | `200314` unlocks `200309` |
| IF2 | `300416` unlocks `300410` |
| Historical victory | `500217` unlocks `500211` |
| IF3 | `500313` unlocks `500309` |
| IF4 | `500320` unlocks `500317` |
| IF5 | `500411` unlocks `500407` |
| IF6 | `500418` unlocks `500414` |
| Dynamic gate A | `500308`: troop > 70 and food > 50 -> `500317`; otherwise -> `500309` |
| Dynamic gate B | `500406`: low risk + strong troop/food -> `500407`; high risk + weak troop/food -> `500414`; otherwise -> `500407` |
| Egg | `500214` -> `500215` -> `500217`; recap badge is gated by the egg flag |
