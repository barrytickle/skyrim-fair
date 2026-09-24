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
Float Property CheerLead = 1.0 Auto
{Seconds before a song's end that its cheer starts, as the last note rings. The song
isn't stopped: it finishes on its own (the update timer can run late when Papyrus is
busy, so waiting for the full length left a gap).}
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
Float[] Property DanceLengths Auto
{Each dance's clip length in seconds. A vanilla idle plays its clip once, so each dancer
is given the next dance the moment the last one ends.}
Idle[] Property CheerIdles Auto
Float[] Property CheerIdleLengths Auto
{Each cheer idle's clip length: a song's "cheer" sections replay them as each ends.}
Idle[] Property ClapIdles Auto
Float[] Property ClapLengths Auto
{A song's "clap" sections: the dancers applaud, each clip replayed as it ends.}
Float Property FaceStageSteps = 3.0 Auto
{Clapping and cheering dancers turn to the stage in this many steps (0.06 s apart), not a snap.}
Int Property DanceEvery = 2 Auto
{Each dancer is given a dance every this many updates during a song (an update is 2 s at most).}

Actor[] Property Singers Auto
{The three singers at the front of the deck (docs/BARDS.md). Their lips follow the song:
each sung line is a topic whose voice file (silent audio, a lip track) differs per
singer's voice type.}
Topic[] Property SingerTopics Auto
Idle[] Property SingerMoves Auto
Float[] Property SingerMoveLengths Auto
{While they sing, the singers gesture: these idles, in turn, each clip replayed after its
length and a breath (SingerGap), the three staggered so they don't move as one.}
Idle[] Property SingerRestMoves Auto
Float[] Property SingerRestLengths Auto
{While the singers rest (a song's singers timeline), these instead (clapping along).}
Idle Property SingerCheerMove Auto
{At a song's end, with the crowd's cheer (a wave).}
Float Property SingerGap = 2.0 Auto
{Seconds each singer stands between moves.}
Float[] Property SingerStarts Auto
{Each line's start, in seconds from its song's start.}
Int[] Property SongFirstLine Auto
{For each song, its first line in SingerTopics, or -1 for a song with no singing.}
Int[] Property SongLineCount Auto

Float[] Property SectionStarts Auto
{Every song's sections, song by song: each one's start in seconds from its song's start.}
Int[] Property SectionDrums Auto
{Retired (the drums alone): a save keeps its old value; SectionPlay has every instrument now.}
Int[] Property SectionPlay Auto
{For each section, each instrument in InstrumentIdles in turn: 0 rest, 1 play, 2 intense.}
Idle[] Property InstrumentIdles Auto
{The instruments with timelines (lute, drum, flute): a musician whose idle is one follows it.}
GlobalVariable[] Property InstrumentTempo Auto
{Each instrument's tempo global (InstrumentIdles order), held at its level: 0 rest, 1 normal,
2 fast. Open Animation Replacer plays a faster loop for the fair's musicians while it's 2.}
Int[] Property SectionSing Auto
{For each section: 1 the singers sing (their lines are said), 0 they rest (lines skipped).}
Int[] Property SectionCrowd Auto
{0 the dancers dance, 1 they clap, 2 they cheer.}
Int[] Property SongFirstSection Auto
{For each song, its first section, or -1 for a song without (drums and dancing throughout).}
Int[] Property SongSectionCount Auto
Idle Property DrumIdle Auto
{Retired: InstrumentIdles has every instrument now.}

GlobalVariable Property AtFair Auto
{1 while the player is at the fair: the compatibility patches switch other mods' per-NPC
spells off while it's on. Saved with the game, so a load at the fair starts with it on.}

Actor[] Property FolkDancers Auto
{Astra's paired folk dance: each dancer's clip replaces FolkIdle's animation for that NPC
(Open Animation Replacer), so both are started together and restarted every FolkLength.}
Idle Property FolkIdle Auto
Float Property FolkLength = 9.6 Auto
{Retired: a save keeps this property's old value, so the length is FolkClipLength now.}
Float Property FolkClipLength = 57.6 Auto
{The folk clip's length in seconds: the pair start it again together as it ends.}

GlobalVariable Property FirstTrack Auto
{For testing: the song to start with on arrival or load (0 = the first), or -1 to carry
on round the playlist. set SkyrimFairAudioFirstTrack to 2 starts with Fiddle.}

FormList Property StripSpells Auto
{The NPC guard's list (SkyrimFairNpcGuard): filled here from StripPlugins/StripIds.}
String[] Property StripPlugins Auto
Int[] Property StripIds Auto

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
; The song finishing under its cheer, and when it ends (the band plays until then).
Int tailInstance = 0
Float bandUntil = 0.0
Float phaseEnds = 0.0
Float ambienceLevel = -1.0
; Which bards are playing, and which archers still wait to be set on their stands (an
; actor whose 3D hasn't loaded yet is caught on a later update).
Bool[] bandPlaying
Bool[] orchestraPlaying
Bool bandOn = False
Int appliedTier = -1
Int danceTick = 0
Float folkNext = 0.0
; Which dancers have started this song's dance (a dance plays once and loops until stopped).
Bool[] dancing
Float[] danceEnds
Int[] dancePlays
Float danceWake = 0.0
Bool folkOn = False
Int songsPlayed = 0
; The song's sung lines: the next one to say, and the end of this song's run.
Float songStarted = 0.0
Int nextLine = -1
Int endLine = -1
; The song's sections: the next to apply, the end of its run, and what's in force.
Int nextSection = -1
Int endSection = -1
Int drumLevel = 1
; Each instrument's level (InstrumentIdles order) and whether the singers sing.
Int[] playLevel
Bool singing = True
Int crowdMode = 0
; Each singer's next move (game time) and how many they've made, and the soonest due.
Float[] singerNext
Int[] singerPlays
Float singerWake = 0.0
Bool[] archerPending
Bool[] archerHeld
Bool holding = False
Float holdEnds = 0.0

Event OnInit()
	Debug.Trace("SkyrimFairAudio: started, " + Songs.Length + " songs")
	FillStripSpells()
	RegisterForSingleUpdate(3.0)
EndEvent

; Called by the player alias on every game load. Whatever was playing is gone.
Function Recover()
	Debug.Trace("SkyrimFairAudio: game loaded, the set starts over")
	songInstance = 0
	cheerInstance = 0
	tailInstance = 0
	bandUntil = 0.0
	phase = 0
	ambienceLevel = -1.0
	bandOn = False
	bandPlaying = new Bool[16]
	orchestraPlaying = new Bool[32]
	appliedTier = -1
	folkNext = 0.0
	FillStripSpells()
	RegisterForSingleUpdate(1.0)
EndFunction

; Other mods' spells the NPC guard takes off the fair's NPCs, from whichever of their
; plugins are loaded (GetFormFromFile returns None for one that isn't).
Function FillStripSpells()
	If !StripSpells
		Return
	EndIf
	StripSpells.Revert()
	Int i = 0
	While i < StripPlugins.Length && i < StripIds.Length
		Spell s = Game.GetFormFromFile(StripIds[i], StripPlugins[i]) as Spell
		If s
			StripSpells.AddForm(s)
		EndIf
		i += 1
	EndWhile
	Debug.Trace("SkyrimFairAudio: NPC guard strips " + StripSpells.GetSize() + " spells")
EndFunction

Event OnUpdate()
	Bool here = Game.GetPlayer().GetWorldSpace() == FairWorld
	If AtFair && (AtFair.GetValue() >= 0.5) != here
		If here
			AtFair.SetValue(1.0)
		Else
			AtFair.SetValue(0.0)
		EndIf
	EndIf
	If !here
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
		If FirstTrack && FirstTrack.GetValue() >= 0.0 && (FirstTrack.GetValue() as Int) < Songs.Length
			track = FirstTrack.GetValue() as Int
		EndIf
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
		Sections(now)
		PlayBand()
		Dance()
		SingerGestures(now)
		FolkDance(now)
		Sing(now)
	ElseIf bandOn && now >= bandUntil
		StopBand(False)
	EndIf

	Float left = Seconds(phaseEnds - now)
	If phase == 2 && danceWake > now && Seconds(danceWake - now) < left
		left = Seconds(danceWake - now)
	EndIf
	If phase == 2 && folkOn && folkNext > now && Seconds(folkNext - now) < left
		left = Seconds(folkNext - now)
	EndIf
	If phase == 2 && singerWake > now && Seconds(singerWake - now) < left
		left = Seconds(singerWake - now)
	EndIf
	If phase != 2 && bandOn && bandUntil > now && Seconds(bandUntil - now) < left
		; Wake as the song's last note ends, to put the instruments away.
		left = Seconds(bandUntil - now)
	EndIf
	If phase == 2 && nextSection >= 0 && nextSection < endSection
		; Wake for the next section.
		Float toSection = SectionStarts[nextSection] - Seconds(now - songStarted)
		If toSection < left
			left = toSection
		EndIf
	EndIf
	If phase == 2 && nextLine >= 0 && nextLine < endLine
		; Wake for the next sung line.
		Float toLine = SingerStarts[nextLine] - Seconds(now - songStarted)
		If toLine < left
			left = toLine
		EndIf
	EndIf
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
		; The last song's tail ended long ago (its cheer and the pause outlast the lead).
		tailInstance = 0
		songInstance = Songs[track].Play(StageSpeaker)
		Debug.Trace("SkyrimFairAudio: song " + track + " playing, instance " + songInstance)
		songsPlayed += 1
		dancing = new Bool[128]
		danceEnds = new Float[128]
		dancePlays = new Int[128]
		folkOn = False
		folkNext = 0.0
		songStarted = now
		nextLine = -1
		endLine = -1
		drumLevel = 1
		playLevel = new Int[8]
		Int pl = 0
		While pl < 8
			playLevel[pl] = 1
			pl += 1
		EndWhile
		SetTempo(1)
		singing = True
		; The singers' first moves, staggered.
		singerNext = new Float[16]
		singerPlays = new Int[16]
		Int si = 0
		While si < 16
			singerNext[si] = now + (1.5 + si * 1.3) * TimeScale.GetValue() / 86400.0
			si += 1
		EndWhile
		crowdMode = 0
		nextSection = -1
		endSection = -1
		If track < SongFirstSection.Length && SongFirstSection[track] >= 0
			nextSection = SongFirstSection[track]
			endSection = nextSection + SongSectionCount[track]
		EndIf
		If track < SongFirstLine.Length && SongFirstLine[track] >= 0
			nextLine = SongFirstLine[track]
			endLine = nextLine + SongLineCount[track]
		EndIf
		Sound.SetInstanceVolume(songInstance, MusicVolume.GetValue())
		Enter(2, SongLengths[track] - Lead(), now)
	ElseIf phase == 2
		; The last note is ringing: the crowd cheers, the song finishes under it, then a breath.
		Float lead = Lead()
		tailInstance = songInstance
		songInstance = 0
		bandUntil = now + lead * TimeScale.GetValue() / 86400.0
		Int cheer = SongCheers[track]
		If cheer >= 0 && cheer < Cheers.Length
			cheerInstance = Cheers[cheer].Play(StageSpeaker)
			Sound.SetInstanceVolume(cheerInstance, CheerVolume.GetValue())
			Debug.Trace("SkyrimFairAudio: cheer " + cheer + " at " + Seconds(now - songStarted) + " s into song " + track + " (" + SongLengths[track] + " s long, lead " + lead + ")")
			Enter(3, CheerLengths[cheer], now)
			Cheer()
		Else
			Enter(4, lead + PauseAfterCheer, now)
		EndIf
	ElseIf phase == 3
		cheerInstance = 0
		Enter(4, PauseAfterCheer, now)
	EndIf
EndFunction

; The cheer's lead, never more than half the song.
Float Function Lead()
	Float lead = CheerLead
	If lead < 0.0
		lead = 0.0
	ElseIf lead > SongLengths[track] / 2.0
		lead = SongLengths[track] / 2.0
	EndIf
	Return lead
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
	nextLine = -1
	If songInstance != 0
		Sound.StopInstance(songInstance)
		songInstance = 0
	EndIf
	If cheerInstance != 0
		Sound.StopInstance(cheerInstance)
		cheerInstance = 0
	EndIf
	If tailInstance != 0
		Sound.StopInstance(tailInstance)
		tailInstance = 0
	EndIf
	bandUntil = 0.0
	SetTempo(1)
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
		; The drummers rest while the drums are calm.
		If !playing[i] && players[i] && players[i].Is3DLoaded() && Playing(idles[i])
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

; Each singer whose next move is due makes it: a gesture while they sing, clapping while they
; rest. A move that doesn't play is tried again a second later.
Function SingerGestures(Float now)
	singerWake = 0.0
	If Singers.Length == 0 || singerNext.Length < Singers.Length
		Return
	EndIf
	Idle[] moves = SingerMoves
	Float[] lengths = SingerMoveLengths
	If !singing
		moves = SingerRestMoves
		lengths = SingerRestLengths
	EndIf
	If moves.Length == 0
		Return
	EndIf
	Float perSecond = TimeScale.GetValue() / 86400.0
	Int i = 0
	While i < Singers.Length
		If now >= singerNext[i] && Singers[i] && Singers[i].Is3DLoaded()
			Int which = (i * 2 + singerPlays[i]) % moves.Length
			Float clip = 3.0
			If which < lengths.Length
				clip = lengths[which]
			EndIf
			If Singers[i].PlayIdle(moves[which])
				singerPlays[i] = singerPlays[i] + 1
				singerNext[i] = now + (clip + SingerGap + i * 0.4) * perSecond
			Else
				singerNext[i] = now + perSecond
			EndIf
		EndIf
		If singerWake == 0.0 || singerNext[i] < singerWake
			singerWake = singerNext[i]
		EndIf
		i += 1
	EndWhile
EndFunction

; Every section whose start has come is applied, from the song's own start (so timer error
; never adds up). A resting instrument's players put theirs away and take it up again after;
; resting singers skip their lines; a change of crowd mode gives each dancer the new mode's
; idle straight away.
Function Sections(Float now)
	If nextSection < 0
		Return
	EndIf
	If playLevel.Length < 8
		playLevel = new Int[8]
		Int pl = 0
		While pl < 8
			playLevel[pl] = 1
			pl += 1
		EndWhile
	EndIf
	Float into = Seconds(now - songStarted)
	Int count = InstrumentIdles.Length
	Int applied = -1
	Int crowd = crowdMode
	While nextSection < endSection && nextSection < SectionStarts.Length && SectionStarts[nextSection] <= into + 0.05
		applied = nextSection
		crowd = SectionCrowd[nextSection]
		nextSection += 1
	EndWhile
	If applied >= 0
		Int k = 0
		While k < count && k < 8
			Int level = SectionPlay[applied * count + k]
			If (level > 0) != (playLevel[k] > 0)
				Debug.Trace("SkyrimFairAudio: instrument " + k + " level " + level + " at " + into + " s")
				If level == 0
					Rest(Band, BandIdles, bandPlaying, InstrumentIdles[k])
					Rest(Orchestra, OrchestraIdles, orchestraPlaying, InstrumentIdles[k])
				EndIf
			EndIf
			If level != playLevel[k] && k < InstrumentTempo.Length && InstrumentTempo[k]
				InstrumentTempo[k].SetValue(level)
			EndIf
			playLevel[k] = level
			k += 1
		EndWhile
		Bool sing = applied >= SectionSing.Length || SectionSing[applied] > 0
		If sing != singing
			Debug.Trace("SkyrimFairAudio: singers " + sing + " at " + into + " s")
		EndIf
		singing = sing
	EndIf
	If crowd != crowdMode
		Debug.Trace("SkyrimFairAudio: crowd " + crowd + " at " + into + " s")
		crowdMode = crowd
		Int i = 0
		While i < dancing.Length
			dancing[i] = False
			i += 1
		EndWhile
		If crowd > 0
			FaceStage()
		EndIf
	EndIf
EndFunction

; Every instrument's tempo global to one level (1: normal, at a song's start and when the set stops).
Function SetTempo(Int level)
	Int k = 0
	While k < InstrumentTempo.Length
		If InstrumentTempo[k] && InstrumentTempo[k].GetValue() != level
			InstrumentTempo[k].SetValue(level)
		EndIf
		k += 1
	EndWhile
EndFunction

; Whether a musician with this idle plays now: an instrument with a timeline follows it.
Bool Function Playing(Idle instrument)
	Int k = 0
	While k < InstrumentIdles.Length && k < playLevel.Length
		If InstrumentIdles[k] == instrument
			Return playLevel[k] > 0
		EndIf
		k += 1
	EndWhile
	Return True
EndFunction

; Every loaded dancer turns to face the stage (its speaker), over a few quick steps. A
; dancer already facing it (within 10 degrees) stays as they are.
Function FaceStage()
	If !StageSpeaker
		Return
	EndIf
	Float[] start = new Float[128]
	Float[] turn = new Float[128]
	Int i = 0
	While i < Dancers.Length && i < 128
		If Dancers[i] && Dancers[i].Is3DLoaded()
			start[i] = Dancers[i].GetAngleZ()
			turn[i] = Dancers[i].GetHeadingAngle(StageSpeaker)
		EndIf
		i += 1
	EndWhile
	Int steps = FaceStageSteps as Int
	If steps < 1
		steps = 1
	EndIf
	Int k = 1
	While k <= steps
		i = 0
		While i < Dancers.Length && i < 128
			If (turn[i] > 10.0 || turn[i] < -10.0) && Dancers[i] && Dancers[i].Is3DLoaded()
				Dancers[i].SetAngle(0.0, 0.0, start[i] + turn[i] * k / steps)
			EndIf
			i += 1
		EndWhile
		If k < steps
			Utility.Wait(0.06)
		EndIf
		k += 1
	EndWhile
EndFunction

Function Rest(Actor[] players, Idle[] idles, Bool[] playing, Idle instrument)
	Int i = 0
	While i < players.Length && i < playing.Length && i < idles.Length
		If idles[i] == instrument && playing[i] && players[i] && players[i].Is3DLoaded()
			players[i].PlayIdle(BandStop)
			playing[i] = False
		EndIf
		i += 1
	EndWhile
EndFunction

; Each dancer starts a dance when the song starts (as soon as their 3D is there), and the
; next as each one ends: a vanilla idle plays its clip once, and re-sending one mid-clip
; restarts it visibly. Dancers take the dances in turn, offset, so the floor varies.
Function Dance()
	; The section's crowd mode picks the idles: dance, clap or cheer.
	Idle[] idles = DanceIdles
	Float[] lengths = DanceLengths
	If crowdMode == 1 && ClapIdles.Length > 0
		idles = ClapIdles
		lengths = ClapLengths
	ElseIf crowdMode == 2 && CheerIdles.Length > 0
		idles = CheerIdles
		lengths = CheerIdleLengths
	EndIf
	If idles.Length == 0
		Return
	EndIf
	If dancing.Length < Dancers.Length
		dancing = new Bool[128]
		danceEnds = new Float[128]
		dancePlays = new Int[128]
	EndIf
	Float now = Utility.GetCurrentGameTime()
	Float perSecond = TimeScale.GetValue() / 86400.0
	danceWake = 0.0
	Int i = 0
	While i < Dancers.Length && i < dancing.Length
		If (!dancing[i] || now >= danceEnds[i]) && Dancers[i] && Dancers[i].Is3DLoaded()
			Int which = (i + dancePlays[i]) % idles.Length
			If Dancers[i].PlayIdle(idles[which])
				dancing[i] = True
				dancePlays[i] = dancePlays[i] + 1
				Float clipSeconds = 6.0
				If which < lengths.Length
					clipSeconds = lengths[which]
				EndIf
				danceEnds[i] = now + clipSeconds * perSecond
			EndIf
		EndIf
		If dancing[i] && (danceWake == 0.0 || danceEnds[i] < danceWake)
			danceWake = danceEnds[i]
		EndIf
		i += 1
	EndWhile
EndFunction

; Every line whose start has come is said by all three singers. Each start is measured
; from the song's own start, so timer error never adds up from line to line.
Function Sing(Float now)
	If nextLine < 0 || Singers.Length == 0
		Return
	EndIf
	Float into = Seconds(now - songStarted)
	While nextLine < endLine && nextLine < SingerStarts.Length && SingerStarts[nextLine] <= into + 0.05
		Int i = 0
		While i < Singers.Length && singing
			If Singers[i] && Singers[i].Is3DLoaded()
				Singers[i].Say(SingerTopics[nextLine])
			EndIf
			i += 1
		EndWhile
		nextLine += 1
	EndWhile
EndFunction

; The folk pair: both start together once both are loaded, and again together as the
; clip ends (it's six loops of Astra's dance, so that's once a minute).
Function FolkDance(Float now)
	If !FolkIdle || FolkDancers.Length == 0 || (folkOn && now < folkNext)
		Return
	EndIf
	Int i = 0
	While i < FolkDancers.Length
		If !FolkDancers[i] || !FolkDancers[i].Is3DLoaded()
			Return
		EndIf
		i += 1
	EndWhile
	i = 0
	While i < FolkDancers.Length
		FolkDancers[i].PlayIdle(FolkIdle)
		i += 1
	EndWhile
	folkOn = True
	folkNext = now + FolkClipLength * TimeScale.GetValue() / 86400.0
EndFunction

; The song's end: the singers wave, and the floor turns to the stage and claps and cheers.
Function Cheer()
	Int s = 0
	While SingerCheerMove && s < Singers.Length
		If Singers[s] && Singers[s].Is3DLoaded()
			Singers[s].PlayIdle(SingerCheerMove)
		EndIf
		s += 1
	EndWhile
	If CheerIdles.Length == 0
		Return
	EndIf
	FaceStage()
	Int i = 0
	While i < Dancers.Length
		If Dancers[i] && Dancers[i].Is3DLoaded()
			Dancers[i].PlayIdle(CheerIdles[i % CheerIdles.Length])
		EndIf
		i += 1
	EndWhile
	nextLine = -1
	; The folk pair stops, and starts afresh with the next song.
	i = 0
	While i < FolkDancers.Length
		If FolkDancers[i] && FolkDancers[i].Is3DLoaded()
			FolkDancers[i].PlayIdle(BandStop)
		EndIf
		i += 1
	EndWhile
	folkOn = False
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
