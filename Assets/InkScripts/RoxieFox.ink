VAR thanked = false
VAR convoCount = 0
VAR fredFoxGold = false
VAR fredFoxHealth = false
VAR fredFoxStamina = false
VAR sneakThief = false

{ sneakThief: -> GoodLuckThief}

{ (fredFoxGold || fredFoxHealth || fredFoxStamina) && convoCount == 0 :
     -> SneakThief
 }
 
{ fredFoxGold && fredFoxHealth && fredFoxStamina:
    -> GoodLuck
}

{ convoCount == 0 :
    -> Entry
- else:
    ~ convoCount += 1
}

{ fredFoxGold || fredFoxHealth || fredFoxStamina: -> KeepLooking}


=== Entry ===
~ convoCount += 1
Kayzie! What are you doing out here?
Hi Roxie! We're looking for our parents! #Okto
Have you seen them? #Okto
Oh no! I haven't seen them, but have you talked to Fred?
We have! He's trying to call them right now. #Okto
He said to come in and say hello. #Okto
Well I just made a care package for Fred's next trip,
you two should take them and go find Faye,
she went out in the forest looking for a rare lilly.
Go to the other room and grab the supplies.
    * [Thanks]
        ~ thanked = true
        -> END
    * [Ok]
        -> END
        
=== SneakThief ===
Kayzie! What are you doing here?
It looks like you found some of Fred's supplies!
    * [Sorry]
        -> Apology
    * [What supplies?]
        -> SneakThief2
-> END

=== GoodLuck ===
Good luck out there you two, and be careful!
Make sure you find Faye, maybe she's seen your parents while she was out!!
-> END

=== Apology ===
Sorry Roxie, Fred told us to come inside and we can't find our parents.. #Kayzie
We woke up and they were just gone! #Okto
Maybe they went to run some errands?
If you've already talked to Fred, then don't worry about the supplies kids. Take what you need!
    * [Thanks]
        ~ thanked = true
        -> END
    * [Alright]
-> END

=== SneakThief2 ===
Those supplies in your pockets young lady!
Okto! What is going on here?!
    * [Apologize yourself]
        -> Apology
    * [Blame Okto]
        -> OktoShame
-> END

=== OktoShame ===
~ sneakThief = true
I'm sorry Mrs. Fox. #Okto
We didn't mean to steal from you. #Okto
Fred told us to come inside and we thought we could use these for our search. #Okto
What search?
We're trying to find our parents, they were gone when we woke up! #Okto
Alright, but you shouldn't be stealing from people!
That will get you no where good kids.
-> END

=== GoodLuckThief ===
Good luck on your search kids.
-> END

=== KeepLooking ===
Make sure you grab all the supplies, they're going to come in handy!
Make sure to check upstairs too!
-> END