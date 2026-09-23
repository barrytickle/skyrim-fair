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

Actor[] Property Orchestra Auto
{The rest of the orchestra (added after the first band, so a save picks them up).}
Idle[] Property OrchestraIdles Auto

ObjectReference[] Property CrowdLayers Auto
{Each crowd layer's enable-parent marker, the most wanted first.}
GlobalVariable Property CrowdLayer Auto
{How many crowd layers are on: set SkyrimFairCrowdLayers to 0..n in the console.}

Actor[] Property Dancers Auto
{The dance floor (persistent references): they dance through each song and cheer at its end.}
Idle[] Property DanceIdles Auto
Idle[] Property CheerIdles Auto
Int Property DanceEvery = 2 Auto
{Each dancer is given a dance every this many updates during a song (an update is 2 s at most).}

Actor[] Property Archers Auto
{The archery range's archers, each linked (unkeyed) to the stand it shoots from.}
GlobalVariable Property ArcherHold Auto
{No longer used to switch the archers (it didn't restart their package); only ever set
back to 0 here, in case a save holds it at 1.}
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
Bool[] orchestraPlaying
Bool bandOn = False
Int appliedTier = -1
Int danceTick = 0
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
	orchestraPlaying = new Bool[32]
	appliedTier = -1
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
	ApplyCrowdLayers()

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
		Dance()
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
			Cheer()
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
	If orchestraPlaying.Length < Orchestra.Length
		orchestraPlaying = new Bool[32]
	EndIf
	bandOn = True
	PlayAll(Band, BandIdles, bandPlaying)
	PlayAll(Orchestra, OrchestraIdles, orchestraPlaying)
EndFunction

Function PlayAll(Actor[] players, Idle[] idles, Bool[] playing)
	Int i = 0
	While i < players.Length && i < playing.Length && i < idles.Length
		If !playing[i] && players[i] && players[i].Is3DLoaded()
			playing[i] = players[i].PlayIdle(idles[i])
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
	If orchestraPlaying.Length < Orchestra.Length
		orchestraPlaying = new Bool[32]
	EndIf
	StopAllOf(Band, bandPlaying, force)
	StopAllOf(Orchestra, orchestraPlaying, force)
	bandOn = False
EndFunction

Function StopAllOf(Actor[] players, Bool[] playing, Bool force)
	Int i = 0
	While i < players.Length && i < playing.Length
		If (force || playing[i]) && players[i] && players[i].Is3DLoaded()
			players[i].PlayIdle(BandStop)
		EndIf
		playing[i] = False
		i += 1
	EndWhile
EndFunction

; The crowd layers on: the first CrowdLayer layers' markers enabled, the rest disabled.
Function ApplyCrowdLayers()
	If !CrowdLayer
		Return
	EndIf
	Int want = CrowdLayer.GetValue() as Int
	If want == appliedTier
		Return
	EndIf
	Int i = 0
	While i < CrowdLayers.Length
		If CrowdLayers[i]
			If i < want
				CrowdLayers[i].Enable()
			Else
				CrowdLayers[i].Disable()
			EndIf
		EndIf
		i += 1
	EndWhile
	appliedTier = want
	Debug.Trace("SkyrimFairAudio: crowd layers on: " + want)
EndFunction

; A share of the dancers takes up a new dance each update, so the floor never moves in
; step, and each dancer moves on through the dances song by song.
Function Dance()
	If DanceIdles.Length == 0
		Return
	EndIf
	danceTick += 1
	Int every = DanceEvery
	If every < 1
		every = 1
	EndIf
	Int i = 0
	While i < Dancers.Length
		If (i + danceTick) % every == 0 && Dancers[i] && Dancers[i].Is3DLoaded()
			Dancers[i].PlayIdle(DanceIdles[(i + danceTick / every) % DanceIdles.Length])
		EndIf
		i += 1
	EndWhile
EndFunction

; The song's end: the floor claps and cheers with the crowd.
Function Cheer()
	If CheerIdles.Length == 0
		Return
	EndIf
	Int i = 0
	While i < Dancers.Length
		If Dancers[i] && Dancers[i].Is3DLoaded()
			Dancers[i].PlayIdle(CheerIdles[i % CheerIdles.Length])
		EndIf
		i += 1
	EndWhile
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
; and re-evaluates. Their package shoots from wherever they stand (see FairWorld.cs), so
; this only squares them up to the target after a load.
Function ResetArchers()
	If ArcherHold && ArcherHold.GetValue() != 0.0
		ArcherHold.SetValue(0.0)
	EndIf
	Int i = 0
	While i < Archers.Length && i < archerPending.Length
		If archerPending[i] && Archers[i] && Archers[i].Is3DLoaded()
			ObjectReference stand = Archers[i].GetLinkedRef()
			If stand
				Archers[i].MoveTo(stand)
			EndIf
			Archers[i].EvaluatePackage()
			archerPending[i] = False
			Debug.Trace("SkyrimFairAudio: archer " + i + " on their stand, running " + Archers[i].GetCurrentPackage())
		EndIf
		i += 1
	EndWhile
EndFunction

Function ReleaseArchers()
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
