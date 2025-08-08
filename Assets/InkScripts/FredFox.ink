VAR hadConvoIndoors = false
VAR hadConvoCareful = false
VAR FredFoxGold = false
VAR FredFoxHealth = false
VAR FredFoxStamina = false
VAR doorOpen = false

{FredFoxGold || FredFoxHealth || FredFoxStamina: -> KeepLooking}
{FredFoxGold && FredFoxHealth && FredFoxStamina: -> AfterRoxie}
{hadConvoIndoors: -> LookingForParents}
{hadConvoCareful: -> ExplorerCareful}
{ not doorOpen: 
    -> Main
- else:
    -> GoInside
}

=== Main ===
~ doorOpen = true
HI Kayzie!
And Okto Too!
What a pleasent surprise,
what are you two doing out here?
    * [I can't find my parents]
        -> LookingForParents
    * [Just exploring]
        -> Explorer1
-> DONE

=== GoInside ===
Go inside and talk to Rosie, she'll have something for you!
-> END

=== LookingForParents ===
~ hadConvoIndoors = true

You can't find your parents?!
Don't worry, I'm sure they're around here somewhere..
Let me call them for you.
While I do that, why don't you and Okto go inside and talk to Roxy!
She'll be excited to see you
-> END

=== Explorer1 ===
You and Okto are out here all alone?
Do your parents know you're out here?
    * [Yes]
        -> ExplorerParentsYes
            -> ExplorerParentsYes
    * [No]
        -> ExplorerParentsNo
-> DONE

=== ExplorerParentsNo ===
Why not?
Where are they?
    * [I don't know]
        -> LookingForParents
    * [At home]
        -> ExplorerParentsYes
-> DONE

=== ExplorerParentsYes ===
~ hadConvoCareful = true

Oh, I see..
Well be sure to tell them I said hello!
And be careful out here,
There are some dangerous creatures in the forest!!
-> END

=== ExplorerCareful ===
Tell your parents I said hello,
And be careful out here,
There are some dangerous creatures in the forest!!
-> END

=== AfterRoxie ===
I don't know where your parents are Kayzie,
but maybe Katie has seen her!
You should go find Katie across the river and ask if she's seen her.
-> END

=== KeepLooking ===
It looks like you didn't find everything we had out for you!
Go back inside and talk to Roxie again, make sure to find everything before you leave!
-> END