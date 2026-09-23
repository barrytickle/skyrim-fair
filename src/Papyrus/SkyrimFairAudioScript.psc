Scriptname SkyrimFairAudioScript extends Quest
{The Wanderer's Fair's stage set: song, cheer, a breath, the next song, round the
playlist, while the player is at the fair. It also sets the level of the fair's crowd
ambience, which plays by itself from placed sound markers, and ducks it under a song.

It also leads the band: the bards take up their instruments when a song starts and put
them away when it ends, as vanilla's bard scenes do (PlayIdle IdleLuteStart, IdleStop).
And as the fair's only controller, it sets the archers back on their stands on every
arrival and load: without navmesh their training package doesn't pick up again by itself.

Every property is written by the generator from fair.config.json (fairWorld.audio);
see docs/AUDIO.md. Sounds do not survive a save or a load, so the player alias calls
Recover() on every load and the set starts over cleanly: nothing is left playing twice.}

WorldSpace Property FairWorld Auto
{Where the set plays. Outside it, everything this script started is stopped.}

ObjectReference Property StageSpeaker Auto
{The persistent marker on the stage the music and the cheer come from.}

Sound[] Property Songs Auto
Float[] Property SongLengths Auto
{Seconds, measured from the files by the generator.}
Int[] Property SongCheers Auto
{Index into Cheers played after each song; -1 for none.}

Sound[] Property Cheers Auto
Float[] Property CheerLengths Auto

Float Property FirstSongDelay = 4.0 Auto
{Seconds after arriving (or loading) before the first song.}
Float Property PauseAfterCheer = 2.0 Auto
{The breath between the cheer and the next song.}
Float Property DuckDuringSong = 0.75 Auto
{The ambience's level while a song plays, as a share of its normal level.}

SoundCategory Property AmbienceCategory Auto
{The fair's own ambience category; the placed crowd markers play in it.}

GlobalVariable Property AmbienceEnabled Auto
GlobalVariable Property AmbienceVolume Auto
GlobalVariable Property MusicEnabled Auto
GlobalVariable Property MusicVolume Auto
GlobalVariable Property CheerVolume Auto
GlobalVariable Property TimeScale Auto
{Vanilla's TimeScale, to turn game time into seconds.}

Actor[] Property Band Auto
{The bards on the stage (persistent references).}
Idle[] Property BandIdles Auto
{For each bard, the idle that brings out their instrument and plays it.}
Idle Property BandStop Auto
{Puts an instrument away.}

Actor[] Property Archers Auto
{The archery range's archers, each linked (unkeyed) to the stand it shoots from.}
GlobalVariable Property ArcherHold Auto
{1 switches the archers to their hold package; back to 0, their training package starts
again from the top (EvaluatePackage alone keeps a package that is already running).}
Float Property ArcherHoldSeconds = 1.5 Auto

Float Property ActivePoll = 2.0 Auto
{Most seconds between checks at the fair (volumes, leaving).}
Float Property IdlePoll = 5.0 Auto
{Seconds between checks while the player is elsewhere.}

; What is playing now. Phases: 0 away, 1 waiting for the first song, 2 song, 3 cheer,
; 4 the pause before the next song.
Int phase = 0
Int track = 0
Int songInstance = 0
Int cheerInstance = 0
Float phaseEnds = 0.0
Float ambienceLevel = -1.0
; Which bards are playing, and which archers still wait to be set on their stands (an
; actor whose 3D hasn't loaded yet is caught on a later update).
Bool[] bandPlaying
Bool bandOn = False
Bool[] archerPending
Bool[] archerHeld
Bool holding = False
Float holdEnds = 0.0

Event OnInit()
	Debug.Trace("SkyrimFairAudio: started, " + Songs.Length + " songs")
	RegisterForSingleUpdate(3.0)
EndEvent

; Called by the player alias on every game load. Whatever was playing is gone.
Function Recover()
	Debug.Trace("SkyrimFairAudio: game loaded, the set starts over")
	songInstance = 0
	cheerInstance = 0
	phase = 0
	ambienceLevel = -1.0
	bandOn = False
	bandPlaying = new Bool[16]
	RegisterForSingleUpdate(1.0)
EndFunction

Event OnUpdate()
	If Game.GetPlayer().GetWorldSpace() != FairWorld
		ReleaseArchers()
		If phase != 0
			StopAll()
			phase = 0
		EndIf
		RegisterForSingleUpdate(IdlePoll)
		Return
	EndIf

	Float now = Utility.GetCurrentGameTime()
	If phase == 0
		; Arrived, or loaded at the fair.
		Enter(1, FirstSongDelay, now)
		StopBand(True)
		QueueArchers()
	EndIf
	ResetArchers()

	If MusicEnabled.GetValue() < 0.5 || Songs.Length == 0
		; Switched off: hold, and start afresh when switched back on.
		If songInstance != 0 || cheerInstance != 0
			StopAll()
		EndIf
		Enter(1, FirstSongDelay, now)
	ElseIf now >= phaseEnds
		Advance(now)
	EndIf

	If songInstance != 0
		Sound.SetInstanceVolume(songInstance, MusicVolume.GetValue())
	EndIf
	SetAmbience(phase == 2)
	If phase == 2
		PlayBand()
	ElseIf bandOn
		StopBand(False)
	EndIf

	Float left = Seconds(phaseEnds - now)
	If left > ActivePoll
		left = ActivePoll
	ElseIf left < 0.1
		left = 0.1
	EndIf
	RegisterForSingleUpdate(left)
EndEvent

Function Advance(Float now)
	If phase == 1 || phase == 4
		If phase == 4
			track = (track + 1) % Songs.Length
		EndIf
		If track >= Songs.Length
			track = 0
		EndIf
		songInstance = Songs[track].Play(StageSpeaker)
		Debug.Trace("SkyrimFairAudio: song " + track + " playing, instance " + songInstance)
		Sound.SetInstanceVolume(songInstance, MusicVolume.GetValue())
		Enter(2, SongLengths[track], now)
	ElseIf phase == 2
		; The song has run its length: the crowd cheers, then a breath.
		Sound.StopInstance(songInstance)
		songInstance = 0
		Int cheer = SongCheers[track]
		If cheer >= 0 && cheer < Cheers.Length
			cheerInstance = Cheers[cheer].Play(StageSpeaker)
			Sound.SetInstanceVolume(cheerInstance, CheerVolume.GetValue())
			Enter(3, CheerLengths[cheer], now)
		Else
			Enter(4, PauseAfterCheer, now)
		EndIf
	ElseIf phase == 3
		cheerInstance = 0
		Enter(4, PauseAfterCheer, now)
	EndIf
EndFunction

Function Enter(Int newPhase, Float lengthSeconds, Float now)
	phase = newPhase
	phaseEnds = now + lengthSeconds * TimeScale.GetValue() / 86400.0
EndFunction

; Game time runs, and stops in menus, as the fair's sounds do; real time does not.
Float Function Seconds(Float days)
	Return days * 86400.0 / TimeScale.GetValue()
EndFunction

Function StopAll()
	If songInstance != 0
		Sound.StopInstance(songInstance)
		songInstance = 0
	EndIf
	If cheerInstance != 0
		Sound.StopInstance(cheerInstance)
		cheerInstance = 0
	EndIf
	SetAmbience(False)
	StopBand(False)
EndFunction

; Each bard not yet playing takes up their instrument, once their 3D is there to play it.
Function PlayBand()
	If bandPlaying.Length < Band.Length
		bandPlaying = new Bool[16]
	EndIf
	bandOn = True
	Int i = 0
	While i < Band.Length && i < bandPlaying.Length
		If !bandPlaying[i] && Band[i] && Band[i].Is3DLoaded()
			bandPlaying[i] = Band[i].PlayIdle(BandIdles[i])
			Debug.Trace("SkyrimFairAudio: bard " + i + " plays: " + bandPlaying[i])
		EndIf
		i += 1
	EndWhile
EndFunction

; Instruments away. With force, every loaded bard is told, playing or not (after a load
; nothing is known about what they hold).
Function StopBand(Bool force)
	If bandPlaying.Length < Band.Length
		bandPlaying = new Bool[16]
	EndIf
	Int i = 0
	While i < Band.Length && i < bandPlaying.Length
		If (force || bandPlaying[i]) && Band[i] && Band[i].Is3DLoaded()
			Band[i].PlayIdle(BandStop)
		EndIf
		bandPlaying[i] = False
		i += 1
	EndWhile
	bandOn = False
EndFunction

Function QueueArchers()
	If archerPending.Length < Archers.Length
		archerPending = new Bool[32]
	EndIf
	Int i = 0
	While i < Archers.Length && i < archerPending.Length
		archerPending[i] = True
		i += 1
	EndWhile
EndFunction

; Each waiting archer whose 3D is loaded goes back onto their stand, facing the target,
; and is switched to the hold package; ArcherHoldSeconds later they're released, and the
; training package starts afresh and shoots.
Function ResetArchers()
	If holding && Utility.GetCurrentGameTime() >= holdEnds
		ReleaseArchers()
	EndIf
	If archerHeld.Length < archerPending.Length
		archerHeld = new Bool[32]
	EndIf
	Bool newly = False
	Int i = 0
	While i < Archers.Length && i < archerPending.Length
		If archerPending[i] && Archers[i] && Archers[i].Is3DLoaded()
			ObjectReference stand = Archers[i].GetLinkedRef()
			If stand
				Archers[i].MoveTo(stand)
			EndIf
			archerPending[i] = False
			archerHeld[i] = True
			newly = True
			Debug.Trace("SkyrimFairAudio: archer " + i + " on their stand, was running " + Archers[i].GetCurrentPackage())
		EndIf
		i += 1
	EndWhile
	If newly && ArcherHold
		ArcherHold.SetValue(1.0)
		i = 0
		While i < Archers.Length && i < archerHeld.Length
			If archerHeld[i]
				Archers[i].EvaluatePackage()
			EndIf
			i += 1
		EndWhile
		holding = True
		holdEnds = Utility.GetCurrentGameTime() + ArcherHoldSeconds * TimeScale.GetValue() / 86400.0
	EndIf
EndFunction

Function ReleaseArchers()
	If !holding
		Return
	EndIf
	ArcherHold.SetValue(0.0)
	Int i = 0
	While i < Archers.Length && i < archerHeld.Length
		If archerHeld[i]
			Archers[i].EvaluatePackage()
			archerHeld[i] = False
			Debug.Trace("SkyrimFairAudio: archer " + i + " released, now running " + Archers[i].GetCurrentPackage())
		EndIf
		i += 1
	EndWhile
	holding = False
EndFunction

Function SetAmbience(Bool duck)
	Float level = 0.0
	If AmbienceEnabled.GetValue() >= 0.5
		level = AmbienceVolume.GetValue()
		If duck
			level *= DuckDuringSong
		EndIf
	EndIf
	If level != ambienceLevel
		AmbienceCategory.SetVolume(level)
		ambienceLevel = level
	EndIf
EndFunction
