INCLUDE MessageLog
INCLUDE PrivateLog
INCLUDE UniversalTaskList
INCLUDE Tutorial
INCLUDE Scene1
INCLUDE Scene2
INCLUDE Finale
INCLUDE Playtesting
INCLUDE Scene3







-> Day_1

=== Day_1 ===
SOV S4, YEAR UNK, MISSION DAY 42 6:00 SCET

"Good waking, Navigator. How was your rest?" 

//Good & Bad and Yes & No will be indicated via UI buttons to keep this communication simple. UI responses will type out full response. 
* (Good_Rest) [Positive Response.] "Glad to receive that." 
    -> Log_Day1
* (Bad_Rest) [Negative Response.] "Sorry to receive that."
    -> Log_Day1 

=== Log_Day1 ===
"New messages, log updates, and tasks await your perusal on the Main Console. Please review them and meet me in the suit when your are ready."
    //These will be made sticky later, single choices for now to keep the player moving forwards 
 + [Check Messages.]
    -> Message_Log.CM_1
 + [Check Private Log.]
    -> Private_Log.PL_1
 + [Check Universal Task List.]
    -> Universal_Task_List.TL_Day_1
 //+ {Message_Log.CM_1} {Private_Log.PL_1} {Universal_Task_List.TL_Day_1} [Enter the Suit] //if all previous options have been visited, show this option. 
 + {Message_Log.CM_1} {Private_Log.PL_1} [Enter the Suit] //if all previous options have been visited, show this option. 
    -> Tutorial

=== Return ===

 -> END
 
