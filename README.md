# A.E.R. Interface
### Audio Interface for Elite Dangerous

Tutorial Video: https://www.youtube.com/watch?v=9Wdi7uVZ87U&feature=youtu.be

A.E.R. or Audio Expository Response Interface, is a voice controlled interface made to simplify playing Elite: Dangerous without leaving the game (like when you play on a Rift).

### Instructions

After being addressed ('Hey Aer'), Aer will continue to listen to commands for 30 seconds. This timer resets after every command. Below is a list of commands supported in 1.3

### Supported Commands (Version 1.3)
  
---
#### A.E.R. Control Related
---
##### Hey Aer
Begins an interaction with A.E.R.

##### [Thank you|Thanks Aer|Nevermind]
Forcefully ends an interaction with A.E.R.

##### Start Listening
Re-enables command processing

##### Stop Listening
Disables Command Processing

##### Stop Talking
Interrupts all speech immediately.

##### What is your current version?
Says the current A.E.R. version

##### Instructions
You can ask for very basic instructions by saying 'I need instructions'

##### Set Commander Name [NATO Alphabet]
Sets your commander name. For Example:

'Set Commander Name Tango Echo India space Leema India November' sets the commander name to Tei Lin.

This is currently only used in greetings.
  
---
#### EDDB Queries
---
##### I need information on [System]
Gives you basic information from EDDB on the system. If AER misunderstands you or doesn't have the system in her database she will tell you.

##### I need information on [Station] in [System]
Gives you detailed information from EDDB on a station.

##### Set local system to [System]
Allows AER to perform commands based on the current system. There is no way outside of the game to directly track where you are, so you have to give AER updates if you want to use some of the commands.

##### How far is [System]
Gives you the distance (in light years) of the system from your current system

##### How far is [System] from [System]
Gives you the distance (in light years) between the two systems.

##### Search for [Commodity]
Finds the closest known station selling the commodity.

##### Where is the nearest Black Market?
Finds the closest known station with a black market

##### Where is the nearest [Federation|Alliance|Empire] System?
Finds the closest known system with the requested allegiance. **Double check against the galaxy map as some of the allegiance information is outdated especially near the borders!!**

##### What is the [Max Landing Pad Size|Known Services|Allegiance] of <station> in <system>
Returns the details asked for. You can substitute 'current' or 'local' system for <system>

##### How far is <station> from the star in <system>?
Tells you the known distance from star for the requested station

##### What is the distance from the star of <station> in <system>?
Another way to access station distances from stars
  
---
#### Typing
---
##### Type System [System]
Types the system name at the cursor

##### Type Spelling [NATO Alphabet]
Types the spelled word at the cursor. Uses the current NATO alphabet.

##### Type That
Types the last word that A.E.R. Spelled at the cursor.

##### Type Current System
Types the current system at the curosr.
  
##### Type Dictation [Words]
Uses Microsoft dictation grammar to allow you to dictate a sentence. 
  
  
---
#### Other
---
##### Greetings
You can greet AER by saying 'Hello' 'Good [Evening|Afternoon|Morning]'

##### Browse Galnet, Next Article, Read Article
Cycles over the Galnet RSS and allows you to read articles. Saying 'Browse Galnet' starts the cycle over at the beginning.

##### Tell A Joke
Connects to a joke RSS and pulls out a joke of the day. (This is currently a bit stale as the RSS doesn't update nearly as much as it should)

##### What time is it?
Says the current real-life time.

##### Calculate [numbers] [times|multiplied by|divided by|plus|minus|to the power of|square root of] [numbers]
Allows you to do simple calculations. [numbers] must be entered digit by digit. For example:

  245.30 * 80, should be entered as "calculate two four five point three zero times eight zero"
    
    
----

## Voices
A.E.R. uses your system's TTS voice. A lot of streamers (and A.S.T.R.A.) use the IVONA Amy voice. I prefer the IVONA Ivy voice. Microsoft has a few low-quality voices available for download (such as Hazel and ZiraPro), but you have to fiddle to get them working. 

A.E.R. is licensed under LGPL 3.0


---- 

##How to Install:
  
###Command Console version 
Extract AERConsole.zip to any folder and run 'AerSpeechConsole.exe'
  
###Voice Attack Plugin 
Extract AERVAPlugin.zip to the VoiceAttack\Apps folder.
The final location should be 'VoiceAttack\Apps\AER'. The AER subfolder is not optional.
