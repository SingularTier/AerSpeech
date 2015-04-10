# A.E.R. Interface
### Audio Interface for Elite Dangerous

A.E.R. or Audio Expository Response Interface, is a voice controlled interface made to simplify playing Elite: Dangerous without leaving the game (like when you play on a Rift).

### Supported Commands (Version 1.2)

#### Start Listening
Re-enables command processing

#### Stop Listening
Disables Command Processing

#### Greetings
You can greet AER by saying 'Hello' 'Good [Evening|Afternoon|Morning]'

#### Browse Galnet, Next Article, Read Article
Cycles over the Galnet RSS and allows you to read articles. Saying 'Browse Galnet' starts the cycle over at the beginning.

#### Tell A Joke
Connects to a joke RSS and pulls out a joke of the day. (This is currently a bit stale as the RSS doesn't update nearly as much as it should)

#### Instructions
You can ask for basic instructions by saying 'I need instructions'

#### I need information on [System]
Gives you basic information from EDDB on the system. If AER misunderstands you or doesn't have the system in her database she will tell you.

#### I need information on [Station] in [System]
Gives you detailed information from EDDB on a station.

#### Set local system to [System]
Allows AER to perform commands based on the current system. There is no way outside of the game to directly track where you are, so you have to give AER updates if you want to use some of the commands.

#### How far is [System]
Gives you the distance (in light years) of the system from your current system

#### How far is [System] from [System]
Gives you the distance (in light years) between the two systems.

#### Search for [Commodity]
Finds the closest known station selling the commodity.

#### Type System [System]
Types the system name at the cursor

#### Type Spelling [NATO Alphabet]
Types the spelled word at the cursor. Uses the current NATO alphabet.

#### Type That
Types the last word that A.E.R. Spelled at the cursor.

#### Type Current System
Types the current system at the curosr.

#### What is your current version?
Says the current A.E.R. version

#### Stop Talking
Interrupts all speech immediately.


### Voices
A.E.R. uses your system's TTS voice. A lot of streamers (and A.S.T.R.A.) use the IVONA Amy voice. I prefer the IVONA Ivy voice. Microsoft has a few low-quality voices available for download (such as Hazel and ZiraPro), but you have to fiddle to get them working. 

A.E.R. is licensed under LGPL 3.0

###How to Install:
  
####Command Console version 
Extract AERConsole.zip to any folder and run 'AerSpeechConsole.exe'
  
####Voice Attack Plugin 
Extract AERVAPlugin.zip to the VoiceAttack\Apps folder.
The final location should be 'VoiceAttack\Apps\AER'. The AER subfolder is not optional.
