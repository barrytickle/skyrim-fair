Scriptname SkyrimFairAudioScript extends Quest
{The Wanderer's Fair's stage set: song, cheer, a breath, the next song, round the
playlist, while the player is at the fair. It also sets the level of the fair's crowd
ambience, which plays by itself from placed sound markers, and ducks it under a song.

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

Event OnInit()
	RegisterForSingleUpdate(3.0)
EndEvent

; Called by the player alias on every game load. Whatever was playing is gone.
Function Recover()
	songInstance = 0
	cheerInstance = 0
	phase = 0
	ambienceLevel = -1.0
	RegisterForSingleUpdate(1.0)
EndFunction

Event OnUpdate()
	If Game.GetPlayer().GetWorldSpace() != FairWorld
		If phase != 0
			StopAll()
			phase = 0
		EndIf
		RegisterForSingleUpdate(IdlePoll)
		Return
	EndIf

	Float now = Utility.GetCurrentGameTime()
	If phase == 0
		Enter(1, FirstSongDelay, now)
	EndIf

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
