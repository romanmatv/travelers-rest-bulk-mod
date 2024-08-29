using System.Collections.Generic;

namespace CustomerChatLines
{
    public partial class Plugin
    {
        enum ConversationGroups
        {
            NeutralInTavern,
            EnterTavernDrink,
            EnterTavernNeutral,
            EnterTavernFood,
            TavernClean,
            TavernDirty,
            TableDirty,
            TavernFilthy,
            Rowdy,
            AcceptRoomFirstFloor,
            AcceptRoomSecondFloor,
            TooCold,
            TooHot,
            BirdNegativeComments,
            BirdPositiveComments,
        }

        private static readonly Dictionary<ConversationGroups, string> Dictionary = new()
        {
            {
                ConversationGroups.BirdPositiveComments, string.Join('|', new[]
                {
                    "What did that bird say?",
                })
            },
            {
                ConversationGroups.BirdNegativeComments,
                string.Join('|', "What did that bird say?", "Such a foul mouth, aint ya, birdie?",
                    "Can I order parrot soup?")
            },
            {
                ConversationGroups.TavernDirty, string.Join('|', new[]
                {
                    "Look at that mess! That's how you get ants!",
                    "The floor is all sticky!",
                    "Eww, brother, eww.",
                })
            },
            {
                ConversationGroups.TableDirty, string.Join('|', new[]
                {
                    "Look at that mess! That's how you get ants!",
                    "Eww, brother, eww.",
                })
            },
            {
                ConversationGroups.TavernFilthy, string.Join('|', new[]
                {
                    "This tavern is dirtier than an outhouse!",
                    "The sewer dungeons did not smell as bad...",
                    "This table is dirtier than MY outhouse",
                    "What is this, a pub for pigs?",
                })
            },
            {
                ConversationGroups.TooCold, string.Join('|', new[]
                {
                    "My teeth are chattering!",
                    "Just a little bit colder, and my drink freezes!",
                    "Okay, it's cold. But the cold never bothered me anyway!",
                    "cough, cough, i think i got some",
                    "Colder than my wife's cooking. Toss a log on the fire would ya?",
                })
            },
            {
                ConversationGroups.TooHot, string.Join('|', new[]
                {
                    "It feels like i'm in the cook pot in here! What fool lit the fire?!",
                })
            },
            {
                ConversationGroups.AcceptRoomFirstFloor, string.Join('|', new[]
                {
                    "I'll be glad to sleep in my own bed again"
                })
            },
            {
                ConversationGroups.AcceptRoomSecondFloor, string.Join('|', new[]
                {
                    "I'll be glad to sleep in my own bed again"
                })
            },
            {
                ConversationGroups.Rowdy, string.Join('|', new[]
                {
                    "Who tossed that at me.",
                    "Today I shall cause problems on purpose.",
                    "Its Raw!",
                    "Oi! Shut up about your pig, mate",
                    "Are you looking at me?",
                    "Hey! What's one gotta do to get attention in here?",
                    "Stop taking food from my plate!",
                    "Hey! That's my drink!",
                    "This is the last you'll see of me!",
                    "This ain't drink, this is slime!",
                    "This ain't food, this is slop!",
                    "I think this meat is people!",
                    "I should have taken my money somewhere else!",
                    "gasp The tavern in town would never!",
                    "I knew I shouldn't have come back here!",
                })
            },
            {
                ConversationGroups.TavernClean, string.Join('|', new[]
                {
                    "This is way better than the last place we went to!",
                    "I'm [redacted] and this is my favourite tavern in the kingdom!",
                    "This must be what old Rygar's place was like",
                    "This place puts old Amos's to shame!",
                    "Let me buy you another drink",
                    "Ye could eat off the floor!",
                })
            },
            {
                ConversationGroups.EnterTavernFood, string.Join('|', new[]
                {
                    "Fish/chicken... why is it always fish/chicken?",
                    "Ahh a hearty meal, now we can bring down that ogre and make some coin!",
                    "This fish is so fresh, me thinks it was just caught.",
                    "I think it blinked.",
                    "From the farm to me mouth, this food is amazing.",
                    "This pork dish is delish... wait...",
                    "They call that well done?",
                    "Smells like Heaven.",
                    "Aye, That's the good stuff",
                    "Reminds me of Mum's cooking",
                    "Almost of good as me wife's cooking",
                    "This dish is so good, it feels like I had it in another life, another place..... Naahhhh",
                    "I came for the food, i stayed for the mood",
                    "I came for the grub, i stayed for the pub",
                    "The food is so good, it's almost worth surviving the dragon attack on the way here",
                    "Is this really home-made? Remarkable.",
                    "This is worse than my spouse's cooking!",
                    "OI! My food is cold! Where's the cook!?",
                    "It's too salty",
                    "This tastes a bit raw",
                    "This tastes like furniture polish.",
                    "Ahhh, I wish I could take some to go.",
                    "My food is still alive!",
                })
            },
            {
                ConversationGroups.EnterTavernNeutral, string.Join('|', new[]
                {
                    "Love me some tea on the road.",
                    "Hear the owner is a bit of a mad one. Tavern looks different every time I come in!",
                    "Thank gods it's open!",
                    "Nearly home now",
                    "This was the 200th time i walked past this street and entered the tavern.",
                    "Hope food's worth the dragon attack",
                    "That storm on the way here was insane!",
                    "The road here was atrocious!",
                })
            },
            {
                ConversationGroups.NeutralInTavern, string.Join('|', new[]
                {
                    "I can't say I've ever heard of a dragon in these parts...",
                    "They forgot the pickles...",
                    "Did the broom just move by itself !? ",
                    "I hear the owner of this place is a cultist!",
                    "Well, the owner seems like the right sort to me.",
                    "we just gonna ignore that magic broom then?",
                    "ye might have a point there.",
                    "Got a cat? Better not be any rats afoot!",
                    "I hear the owner likes to chase them chickens... obsessed with them eggs I tell ya.",
                    "Smells horrid out there, what a horrid smelling wind out there.",
                    "Did you fart? Don’t blame the dog either!",
                    "Heard the lad/lass who runs this place was a bandit",
                    "Wait'll you hear what I heard about the barkeep.....",
                    "I heard the owner here is runaway royalty!",
                    "Wasn't this place built on a grave yard?",
                    "Those Flying Brooms sure seem handy. ",
                    "I'll have the Stella roasted pork please...",
                    "Only Cultists use Flying Brooms!",
                    "The Waitress is really cute.",
                    "Buck up lads, we're almost there",
                    "I'll be glad to sleep in my own bed again",
                    // You could swap out dragon attack with a bunch of things like goblin ambush/raid or troll attack
                    "Behave or you'll get mopped",
                    "Why is there a cat in here?",
                    "Not him again!",
                    "Why are the squirrels so cute around here?",
                    "i saw some weirdo swinging a broom at a turkey. who does that?",
                    "My cousin was a turkey once",
                    "Who let that one in here?",
                    "I came for the ale, i stayed for the tale",
                    "Did you hear about the miner stuck in the ice?",
                    "Poor fella, frozen solid",
                    "Did you see the giant rock man?", "Heard he has a chicken for a hat",
                    "I wonder what the nice family south of here sells?",
                    "Did you see that lass at the beach?",
                    "Some creepy guy wanted to sell me a bird",
                    "My cousin told me I had to try this place.",
                    "Next time, I will bring a fish for the kitty",
                    "Me smells the scent of freshly cooked fish.",
                    "Do coconuts migrate?",
                    "I'm telling ya, that wild turkey gave me a funny look!",
                    "Know where I can have a bit of fun?",
                    "I hear the owner here hates cultists!",
                    "I heard [Random olde bard name] would be playing here",
                    "Everything was better in the old days",
                    "This place has mushroom for improvement",
                    "I heard that the owner here catches the fish emself.",
                    "He saw it with his own eyes.",
                    "It's my birthday today.",
                    "What has the cat been eating, me thinks it farted.",
                    "I wonder when the bridge will be rebuilt?",
                    "much better than the one in the city",
                    "What a fantastic day to break the 4th wall!",
                    "This place be just better than camping outside!",
                    "Ye know, I was there when the tavern opened!",
                    "I saw a moving castle in the wastes yesterday.",
                    "My turnip-head scarecrow went missing.",
                    "I saw some strange lights in the forest last night.",
                    "Um... So good!",
                    "I wanted to enjoy the beach but I sat on a pair of underwear!",
                    "Be prepared, tomorrow we have a long journey to go.",
                    "What ?! You're going alone tomorrow ! For once we find a clean tavern with great meals...",
                    "He's not wrong...",
                    "Ho ! I heard it's the owner who cooks here.",
                    "…and that’s how we awakened Cthulhu. Anyway, the food here is pretty good.",
                    "Why do you think that ems is part of a cult?",
                    "Ya think those rumors are true?",

                    "Strange type, don't ya think? No wonder it's believed ems a cultist.",
                    "I've overheard Farmer Buzz speaking to the cows yesterday",
                    "That cat looks cute, i wonder if i can pet it?", //  - can be adjusted to dog if/when implemented.
                    "I used to be an adventurer like you. Then i took an arrow to the knee...",
                    "That one armed bandit robbed me last week.",
                    "That commission almost killed me, I need a drink because I make it out alive",
                    "How I lost my eye? ... I ran into an EYEcatching monster.",
                    "I tried the cow stomach stew the other week, it was offal!",
                    "Have you heard there is a man frozen to the north?",
                    "Doth mother know you weareth her drapes?",
                    "Someone’s been digging holes by the river, the beach, …everywhere",
                    "It good to see the old shack turn into such a fine Tavern!",
                    "Try the Veal.",
                    "Bless your heart.",
                    "You know, I heard that x and y finally confessed to eachother",
                    "I wish I were alive during Reggi's age",
                    "This tastes like boiled charcoal.",
                    "Can I order parrot soup?",
                    "Oh hey, my favorite drinking buddy! Let's get some mead!",
                    "You wanna know how I got these scars?",
                    "the soup is cold and the salad is hot how is that even possible?",

                    "Blücher!", "Bless you",
                    "... Did you just hear some horses neighing?",
                })
            },
            {
                ConversationGroups.EnterTavernDrink, string.Join('|', new[]
                {
                    "Drink up me hearties",
                    "I hope the barkeep don hic cut me off",
                    "We should buy a Tavern",
                    "Tastes like Piss!",
                    "This is the best drink i ever had!",
                    "I drink, therefore I am.",
                    "I want to drink till I forget I'm drinking!",
                    "Fruity beer, this is wow.",
                    "I mead a refill",
                    "Drink that you may live!",
                    "May your hair grow grey!",
                    "To your health!",
                    "No we didn't eat him you oaf.",
                    "To food, friends, and fire!",
                    "Fortune and glory!",
                    "May the bards remember our tale!",
                    "A long life to ye, and a short one to yer foes!",
                    "Good fortune for all... except those goblins...crafty devils",
                    "Someone must have spit in this drink",
                    "Wow! This tastes almost as good as that manticore we ate yesterday!",
                    "Aw.. I spilled my drink..",
                    "Oops, the beer went down the drain ... Should have ordered 2",
                    "I don't want to know what drain exactly",
                    "Did someone forget to ferment this grape juice?",
                })
            }
        };
    }
}