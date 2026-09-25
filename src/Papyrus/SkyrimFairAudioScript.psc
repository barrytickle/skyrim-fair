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

; The show's data carries a 2 in its names (2026-09-25): a save keeps a script's property values
; from the first time it ran, so the new playlist, idles and holds never reached an existing
; save under the old names. New names take the plugin's values. Rename again (3, ...) when a
; later update must reach saves already playing.
Sound[] Property Songs2 Auto
Float[] Property SongLengths2 Auto
{Seconds, measured from the files by the generator.}
Int[] Property SongCheers2 Auto
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
String[] Property MoreDanceEvents Auto
Float[] Property MoreDanceLengths Auto
{Professional Dancer's dances: its animation events (Dance1...) and their clips' lengths. While
dancing, if its plugin is loaded, each dancer takes them in turn; otherwise the vanilla dances.}
String Property MoreDancesPlugin = "Dance.esp" Auto
Int Property MoreDancesCheck Auto
{A record in MoreDancesPlugin that shows it's loaded.}

ObjectReference[] Property FireworkSites Auto
{Invisible markers the fireworks go up from, as each song ends.}
ObjectReference[] Property FireworkAims Auto
{A marker above each site, for the shell to fly at.}
Spell[] Property FireworkShells Auto
{The shells, one per site in turn (the fair's own: vanilla effects, no damage).}
Spell[] Property FireworkNightShells Auto
{More at night (20:00-05:00), from the middle site.}
Float Property FireworkStagger = 0.6 Auto
GlobalVariable Property FireworksOn Auto
{set SkyrimFairFireworks to 0 to stop them.}

Float Property FaceStageSteps = 3.0 Auto
{Clapping and cheering dancers turn to the stage in this many steps (0.06 s apart), not a snap.}
Int Property DanceEvery = 2 Auto
{Each dancer is given a dance every this many updates during a song (an update is 2 s at most).}

Actor[] Property Singers Auto
{The three singers at the front of the deck (docs/BARDS.md). Their lips follow the song:
each sung line is a topic whose voice file (silent audio, a lip track) differs per
singer's voice type.}
Topic[] Property SingerTopics2 Auto
Idle[] Property SingerMoves Auto
Float[] Property SingerMoveLengths Auto
{While they sing, the singers gesture: these idles, in turn, each clip replayed after its
length and a breath (SingerGap), the three staggered so they don't move as one.}
Idle[] Property SingerRestMoves Auto
Float[] Property SingerRestLengths Auto
{While the singers rest (a song's singers timeline), these instead (clapping along).}
Idle Property SingerCheerMove Auto
{Retired: the song-end move is SingerEndMove now.}
Idle[] Property SingerCheerMoves Auto
Float[] Property SingerCheerLengths Auto
{A "cheer" stretch of the singers' timeline: they break into these (the Civil War cheer) at
once, replayed through the stretch, and keep singing their lines.}
Idle Property SingerEndMove Auto
{At a song's end, with the crowd's cheer (a wave).}
Float Property SingerGap = 2.0 Auto
{Seconds each singer stands between moves.}
Actor Property SingerAnchor Auto
{Hidden under the deck, facing north, its AI off. While the show runs the singers keep an
offset from it (KeepOffsetFromActor), so they step sideways across the deck facing the crowd.}
Float[] Property SingerHomeX Auto
Float[] Property SingerHomeY Auto
Float[] Property SingerHomeZ Auto
{Each singer's mark, as an offset from the anchor.}
Float[] Property SingerFacing Auto
{Each singer's heading, in degrees from the anchor's.}
Float[] Property SingerStepOffsets Auto
{The line's sideways offsets, in turn, one a step (Songs2.config.json singerSteps). None: they stand.}
Float Property SingerStepEvery = 8.0 Auto
{Seconds between steps while they sing.}
Float Property SingerStepSeconds = 3.0 Auto
{Seconds a step takes: no gesture starts before it ends, or would run into the next.}
Float Property SingerFirstStep = 6.0 Auto
{Seconds into a song before the first step.}
Float Property SingerCatchUp = 1000.0 Auto
Float Property SingerFollow = 12.0 Auto
{KeepOffsetFromActor's radii: past catch-up they'd run; within follow they stand.}
Float[] Property SingerStarts2 Auto
{Each line's start, in seconds from its song's start.}

Quest Property CompanionFinder Auto
{Optional aliases that find the player's companions anywhere (teammates or followers, alive,
not waiting). Followers don't come through the fair's gate by themselves: its load door has
no navmesh door links on either side. So on arriving and on leaving, the finder is restarted
and each companion it finds is moved to the player.}
Int Property CompanionSlots = 6 Auto

Actor[] Property Cameos Auto
{The named ambient characters (Garrick Sol V, Claudius Vale; FairCameos.cs). They wander on their
own sandbox; every so often, when free, each plays his next idle and stops it after its hold.}
Idle[] Property CameoIdles2 Auto
Float[] Property CameoHolds2 Auto
Idle[] Property CameoStops2 Auto
{The idle that ends each of CameoIdles2 (Hadvar's ledger has its own exit); None: BandStop.}
Int[] Property CameoFirstIdle2 Auto
Int[] Property CameoIdleCount2 Auto
{Each cameo's idles are CameoIdles2[CameoFirstIdle2[i] ...], CameoIdleCount2[i] of them, in turn.}
Float[] Property CameoEveryMin2 Auto
Float[] Property CameoEveryMax2 Auto
{Seconds between one cameo's idles, at random between the two.}
GlobalVariable Property CameoDuckUntil Auto
{Set by each cameo line's begin fragment (SkyrimFairCameoLine): the real time its line ends.}
Float Property CameoDuck = 0.4 Auto
{The music's volume, as a share, while the player talks to a cameo (0.4, about 8 dB down).}
GlobalVariable[] Property CameoSpot Auto
Int[] Property CameoSpotCount Auto
Float[] Property CameoMoveMin Auto
Float[] Property CameoMoveMax Auto
{Each cameo's round: his spot global (his spot packages each run on one value), how many
spots, and the seconds before he moves on. Moving on sets the global to another spot and
re-evaluates his package, so he walks there.}
Int[] Property SongFirstLine2 Auto
{For each song, its first line in SingerTopics2, or -1 for a song with no singing.}
Int[] Property SongLineCount2 Auto

Float[] Property SectionStarts2 Auto
{Every song's sections, song by song: each one's start in seconds from its song's start.}
Int[] Property SectionDrums Auto
{Retired (the drums alone): a save keeps its old value; SectionPlay2 has every instrument now.}
Int[] Property SectionPlay2 Auto
{For each section, each instrument in InstrumentIdles in turn: 0 rest (held, still), 1 normal,
2 fast, 3 away (put away).}
Idle[] Property InstrumentIdles Auto
{The instruments with timelines (lute, drum, flute): a musician whose idle is one follows it.}
GlobalVariable[] Property InstrumentTempo Auto
{Each instrument's tempo global (InstrumentIdles order), held at its level: 0 rest, 1 normal,
2 fast, 3 away. Open Animation Replacer plays a held loop for the fair's musicians while it's
0 and a faster one while it's 2.}
Int[] Property SectionSing2 Auto
{For each section: 1 the singers sing (their lines are said), 2 they sing and cheer, 0 they
rest (lines skipped).}
Int[] Property SectionCrowd2 Auto
{0 the dancers dance, 1 they clap, 2 they cheer.}
Int[] Property SongFirstSection2 Auto
{For each song, its first section, or -1 for a song without (drums and dancing throughout).}
Int[] Property SongSectionCount2 Auto
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
{For testing: the song to start with on arrival or load (0 = the first). At -1 (the default)
the show still opens with the first song, The Wanderer's Fair. set SkyrimFairAudioFirstTrack
to 3 starts with Fiddle.}

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
; The show's clock, in game-day units like the rest: real seconds, so it keeps pace with the
; music when the engine lets game time fall behind (long frames), but never more than a little
; over the game time that passed, so a menu (which pauses the music too) isn't counted.
Float showDays = 0.0
Float lastReal = 0.0
Float lastGame = 0.0
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
; The crowd mode each dancer's last move was from.
Int[] danceMode
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
Int singMode = 1
Int crowdMode = 0
; Each singer's next move (game time) and how many they've made, and the soonest due.
Float[] singerNext
Int[] singerPlays
Float singerWake = 0.0
; The singers' steps: the offset in force (SingerStepOffsets index), when the next is due and
; when the last ends (game time), and whether their offset is held at all.
Int stepAt = 0
Float stepNext = 0.0
Float stepDone = 0.0
Float stepApplied = 0.0
Bool stepOn = False
; Whether Professional Dancer is loaded (looked up once a load), and who's in one of its dances
; (a looping animation, stopped with IdleForceDefaultState).
Bool moreDancesChecked = False
Bool moreDancesFound = False
Bool[] inMoreDance
Bool[] archerPending
Bool[] archerHeld
Bool holding = False
; The cameos: each one's next change (game time), whether an idle is playing, how many he's made.
Float[] cameoNext
Bool[] cameoPlaying
Int[] cameoPlays
Int[] cameoIdleNow
Float[] cameoMoveNext
Float cameoWake = 0.0
Float holdEnds = 0.0

Event OnInit()
	Debug.Trace("SkyrimFairAudio: started, " + Songs2.Length + " Songs2")
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
	lastReal = 0.0
	phase = 0
	ambienceLevel = -1.0
	bandOn = False
	bandPlaying = new Bool[16]
	orchestraPlaying = new Bool[32]
	appliedTier = -1
	folkNext = 0.0
	moreDancesChecked = False
	cameoNext = new Float[8]
	cameoPlaying = new Bool[8]
	cameoPlays = new Int[8]
	cameoIdleNow = new Int[8]
	cameoMoveNext = new Float[8]
	If CameoDuckUntil
		; Real time starts again from nothing at each launch: a saved deadline would duck for good.
		CameoDuckUntil.SetValue(0.0)
	EndIf
	SingersStand()
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
			; Just left the fair: companions left inside come out too.
			BringCompanions()
		EndIf
		RegisterForSingleUpdate(IdlePoll)
		Return
	EndIf

	Float now = Utility.GetCurrentGameTime()
	; The music's schedule runs on the show clock; the animations (their clips) on game time.
	Float show = ShowNow()
	If phase == 0
		; Arrived, or loaded at the fair.
		; The show opens with the first song (The Wanderer's Fair), wherever the save left off.
		track = 0
		If FirstTrack && FirstTrack.GetValue() >= 0.0 && (FirstTrack.GetValue() as Int) < Songs2.Length
			track = FirstTrack.GetValue() as Int
		EndIf
		Enter(1, FirstSongDelay, show)
		StopBand(True)
		QueueArchers()
		; Arrived (or loaded here): companions left outside come in.
		BringCompanions()
	EndIf
	ResetArchers()
	ApplyCrowdLayers()
	CameoIdles2(Utility.GetCurrentGameTime())

	If MusicEnabled.GetValue() < 0.5 || Songs2.Length == 0
		; Switched off: hold, and start afresh when switched back on.
		If songInstance != 0 || cheerInstance != 0
			StopAll()
		EndIf
		Enter(1, FirstSongDelay, show)
	ElseIf show >= phaseEnds
		Advance(show)
	EndIf

	If songInstance != 0
		If CameoTalking()
			Sound.SetInstanceVolume(songInstance, MusicVolume.GetValue() * CameoDuck)
		Else
			Sound.SetInstanceVolume(songInstance, MusicVolume.GetValue())
		EndIf
	EndIf
	SetAmbience(phase == 2)
	If phase == 2
		Sections(show)
		PlayBand()
		Dance()
		SingerSteps(now)
		SingerGestures(now)
		FolkDance(now)
		Sing(show)
	Else
		If phase == 3
			Dance()
		EndIf
		If bandOn && show >= bandUntil
			StopBand(False)
		EndIf
	EndIf

	Float left = Seconds(phaseEnds - show)
	If (phase == 2 || phase == 3) && danceWake > now && Seconds(danceWake - now) < left
		left = Seconds(danceWake - now)
	EndIf
	If phase == 2 && folkOn && folkNext > now && Seconds(folkNext - now) < left
		left = Seconds(folkNext - now)
	EndIf
	If phase == 2 && singerWake > now && Seconds(singerWake - now) < left
		left = Seconds(singerWake - now)
	EndIf
	If phase == 2 && SingerStepOffsets.Length > 0 && stepNext > now && Seconds(stepNext - now) < left
		left = Seconds(stepNext - now)
	EndIf
	If cameoWake > now && Seconds(cameoWake - now) < left
		left = Seconds(cameoWake - now)
	EndIf
	If left > 0.5 && CameoNear()
		; A cameo close by: look again soon, so the music ducks as soon as he's spoken to.
		left = 0.5
	EndIf
	If phase != 2 && bandOn && bandUntil > show && Seconds(bandUntil - show) < left
		; Wake as the song's last note ends, to put the instruments away.
		left = Seconds(bandUntil - show)
	EndIf
	If phase == 2 && nextSection >= 0 && nextSection < endSection
		; Wake for the next section.
		Float toSection = SectionStarts2[nextSection] - Seconds(show - songStarted)
		If toSection < left
			left = toSection
		EndIf
	EndIf
	If phase == 2 && nextLine >= 0 && nextLine < endLine
		; Wake for the next sung line.
		Float toLine = SingerStarts2[nextLine] - Seconds(show - songStarted)
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
			track = (track + 1) % Songs2.Length
		EndIf
		If track >= Songs2.Length
			track = 0
		EndIf
		; The last song's tail ended long ago (its cheer and the pause outlast the lead).
		tailInstance = 0
		songInstance = Songs2[track].Play(StageSpeaker)
		Debug.Trace("SkyrimFairAudio: song " + track + " playing, instance " + songInstance)
		songsPlayed += 1
		dancing = new Bool[128]
		danceEnds = new Float[128]
		dancePlays = new Int[128]
		danceMode = new Int[128]
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
		singMode = 1
		; The singers' first moves, staggered.
		singerNext = new Float[16]
		singerPlays = new Int[16]
		Int si = 0
		While si < 16
			singerNext[si] = Utility.GetCurrentGameTime() + (1.5 + si * 1.3) * TimeScale.GetValue() / 86400.0
			si += 1
		EndWhile
		; The line starts on its marks and takes its first step a few seconds in.
		stepAt = 0
		stepOn = False
		stepDone = 0.0
		stepNext = Utility.GetCurrentGameTime() + SingerFirstStep * TimeScale.GetValue() / 86400.0
		crowdMode = 0
		nextSection = -1
		endSection = -1
		If track < SongFirstSection2.Length && SongFirstSection2[track] >= 0
			nextSection = SongFirstSection2[track]
			endSection = nextSection + SongSectionCount2[track]
		EndIf
		If track < SongFirstLine2.Length && SongFirstLine2[track] >= 0
			nextLine = SongFirstLine2[track]
			endLine = nextLine + SongLineCount2[track]
		EndIf
		Sound.SetInstanceVolume(songInstance, MusicVolume.GetValue())
		Enter(2, SongLengths2[track] - Lead(), now)
	ElseIf phase == 2
		; The last note is ringing: the crowd cheers, the song finishes under it, then a breath.
		Float lead = Lead()
		tailInstance = songInstance
		songInstance = 0
		bandUntil = now + lead * TimeScale.GetValue() / 86400.0
		Int cheer = SongCheers2[track]
		If cheer >= 0 && cheer < Cheers.Length
			cheerInstance = Cheers[cheer].Play(StageSpeaker)
			Sound.SetInstanceVolume(cheerInstance, CheerVolume.GetValue())
			Debug.Trace("SkyrimFairAudio: cheer " + cheer + " at " + Seconds(now - songStarted) + " s into song " + track + " (" + SongLengths2[track] + " s long, lead " + lead + ")")
			Enter(3, CheerLengths[cheer], now)
			Cheer()
			Fireworks()
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
	ElseIf lead > SongLengths2[track] / 2.0
		lead = SongLengths2[track] / 2.0
	EndIf
	Return lead
EndFunction

; The show clock (see showDays): each call adds the real seconds since the last, capped at 1.6x
; the game time that passed plus a quarter second. The log showed game time 12-35% behind real
; time during a song (2026-09-24), so the cheer came 10-35 s after the music had ended.
Float Function ShowNow()
	Float realNow = Utility.GetCurrentRealTime()
	Float gameNow = Utility.GetCurrentGameTime()
	Float perSecond = TimeScale.GetValue() / 86400.0
	If lastReal > 0.0 && realNow >= lastReal
		Float step = realNow - lastReal
		Float cap = (gameNow - lastGame) / perSecond * 1.6 + 0.25
		If step > cap
			step = cap
		EndIf
		If step > 0.0
			showDays += step * perSecond
		EndIf
	EndIf
	lastReal = realNow
	lastGame = gameNow
	Return showDays
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
	Int i = 0
	While i < Dancers.Length && i < inMoreDance.Length
		If inMoreDance[i] && Dancers[i] && Dancers[i].Is3DLoaded()
			Debug.SendAnimationEvent(Dancers[i], "IdleForceDefaultState")
		EndIf
		inMoreDance[i] = False
		i += 1
	EndWhile
	SetAmbience(False)
	StopBand(False)
	SingersStand()
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
	ElseIf singMode == 2 && SingerCheerMoves.Length > 0
		moves = SingerCheerMoves
		lengths = SingerCheerLengths
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
			If now < stepDone
				; Mid-step: a full-body idle would stop the walk.
				singerNext[i] = stepDone
			ElseIf singing && SingerStepOffsets.Length > 0 && stepOn && now + clip * perSecond > stepNext
				; It wouldn't end before the next step: wait until that one's done.
				singerNext[i] = stepNext + SingerStepSeconds * perSecond
			ElseIf Singers[i].PlayIdle(moves[which])
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

; Every companion the finder turns up that isn't already near the player is moved to them,
; a step behind and to the side. The finder is stopped again after.
Function BringCompanions()
	If !CompanionFinder
		Return
	EndIf
	CompanionFinder.Stop()
	If !CompanionFinder.Start()
		Return
	EndIf
	Actor player = Game.GetPlayer()
	Int brought = 0
	Int i = 0
	While i < CompanionSlots
		ReferenceAlias slot = CompanionFinder.GetAlias(i) as ReferenceAlias
		Actor mate = None
		If slot
			mate = slot.GetActorRef()
		EndIf
		If mate && mate != player && (mate.GetWorldSpace() != player.GetWorldSpace() || mate.GetDistance(player) > 1500.0)
			mate.MoveTo(player, 70.0 * ((brought % 3) - 1), -90.0 - 40.0 * (brought / 3), 0.0)
			brought += 1
		EndIf
		i += 1
	EndWhile
	CompanionFinder.Stop()
	If brought > 0
		Debug.Trace("SkyrimFairAudio: brought " + brought + " companions to the player")
	EndIf
EndFunction

; Whether the player is talking to a cameo, and whether one is close enough to be spoken to.
Bool Function CameoTalking()
	If CameoDuckUntil && Utility.GetCurrentRealTime() < CameoDuckUntil.GetValue()
		Return True
	EndIf
	Int i = 0
	While i < Cameos.Length
		If Cameos[i] && Cameos[i].Is3DLoaded() && Cameos[i].IsInDialogueWithPlayer()
			Return True
		EndIf
		i += 1
	EndWhile
	Return False
EndFunction

Bool Function CameoNear()
	Actor player = Game.GetPlayer()
	Int i = 0
	While i < Cameos.Length
		If Cameos[i] && Cameos[i].Is3DLoaded() && Cameos[i].GetDistance(player) < 600.0
			Return True
		EndIf
		i += 1
	EndWhile
	Return False
EndFunction

; The cameos' idles. Each waits a random while (CameoEveryMin2..Max), then, if he's loaded and
; free (not sitting, fighting or talking to the player), plays his next idle; after its hold
; he's stopped (BandStop) and handed back to his package. Busy, he's tried again in 10 s.
Function CameoIdles2(Float now)
	cameoWake = 0.0
	If Cameos.Length == 0
		Return
	EndIf
	If cameoNext.Length < 8
		cameoNext = new Float[8]
		cameoPlaying = new Bool[8]
		cameoPlays = new Int[8]
	EndIf
	If cameoIdleNow.Length < 8
		cameoIdleNow = new Int[8]
	EndIf
	If cameoMoveNext.Length < 8
		cameoMoveNext = new Float[8]
	EndIf
	Float perSecond = TimeScale.GetValue() / 86400.0
	Int i = 0
	While i < Cameos.Length && i < 8
		Actor a = Cameos[i]
		If cameoNext[i] == 0.0
			cameoNext[i] = now + Utility.RandomFloat(CameoEveryMin2[i], CameoEveryMax2[i]) * perSecond
		ElseIf now >= cameoNext[i] && a
			If cameoPlaying[i]
				If a.Is3DLoaded()
					Idle stop = BandStop
					Int playing = cameoIdleNow[i]
					If playing < CameoStops2.Length && CameoStops2[playing]
						stop = CameoStops2[playing]
					EndIf
					a.PlayIdle(stop)
					a.EvaluatePackage()
				EndIf
				cameoPlaying[i] = False
				cameoNext[i] = now + Utility.RandomFloat(CameoEveryMin2[i], CameoEveryMax2[i]) * perSecond
			ElseIf CameoIdleCount2[i] > 0 && a.Is3DLoaded() && !a.IsInCombat() && a.GetSitState() == 0 && !a.IsInDialogueWithPlayer()
				Int k = CameoFirstIdle2[i] + cameoPlays[i] % CameoIdleCount2[i]
				If a.PlayIdle(CameoIdles2[k])
					cameoIdleNow[i] = k
					cameoPlays[i] = cameoPlays[i] + 1
					cameoPlaying[i] = True
					cameoNext[i] = now + CameoHolds2[k] * perSecond
				Else
					cameoNext[i] = now + 5.0 * perSecond
				EndIf
			Else
				cameoNext[i] = now + 10.0 * perSecond
			EndIf
		EndIf
		If cameoWake == 0.0 || cameoNext[i] < cameoWake
			cameoWake = cameoNext[i]
		EndIf
		; His round: when it's time and he isn't mid-idle, on to another spot.
		If a && i < CameoSpotCount.Length && CameoSpotCount[i] > 1 && i < CameoSpot.Length && CameoSpot[i]
			If cameoMoveNext[i] == 0.0
				cameoMoveNext[i] = now + Utility.RandomFloat(CameoMoveMin[i], CameoMoveMax[i]) * perSecond
			ElseIf now >= cameoMoveNext[i] && !cameoPlaying[i]
				Int count = CameoSpotCount[i]
				Int spot = ((CameoSpot[i].GetValue() as Int) + 1 + Utility.RandomInt(0, count - 2)) % count
				CameoSpot[i].SetValue(spot)
				If a.Is3DLoaded()
					a.EvaluatePackage()
				EndIf
				cameoMoveNext[i] = now + Utility.RandomFloat(CameoMoveMin[i], CameoMoveMax[i]) * perSecond
			EndIf
			If cameoMoveNext[i] < cameoWake
				cameoWake = cameoMoveNext[i]
			EndIf
		EndIf
		i += 1
	EndWhile
EndFunction

; The singers step as a line: while they sing, every SingerStepEvery seconds the next offset in
; SingerStepOffsets; resting, back to their marks (to clap there). The first call of a song puts
; them on their offset from the anchor, which faces them to the crowd.
Function SingerSteps(Float now)
	If SingerStepOffsets.Length == 0 || !SingerAnchor || SingerHomeX.Length < Singers.Length
		Return
	EndIf
	Float perSecond = TimeScale.GetValue() / 86400.0
	If !stepOn
		stepAt = 0
		SingerOffset(SingerStepOffsets[0])
	EndIf
	If !singing
		If stepApplied != 0.0
			SingerOffset(0.0)
			HoldGestures(now + SingerStepSeconds * perSecond)
		EndIf
		; After a rest, a full interval before the next step.
		stepNext = now + SingerStepEvery * perSecond
		Return
	EndIf
	If now >= stepNext
		stepAt = (stepAt + 1) % SingerStepOffsets.Length
		SingerOffset(SingerStepOffsets[stepAt])
		HoldGestures(now + SingerStepSeconds * perSecond)
		stepNext = now + SingerStepEvery * perSecond
	EndIf
EndFunction

; Every singer keeps his mark's offset from the anchor, moved sideways by dx.
Function SingerOffset(Float dx)
	If !SingerAnchor || SingerHomeX.Length < Singers.Length
		Return
	EndIf
	If SingerAnchor.Is3DLoaded()
		; Neither is sure to last a reload of its 3D, so both are set every time.
		SingerAnchor.EnableAI(False)
		SingerAnchor.SetAlpha(0.0)
	EndIf
	Int i = 0
	While i < Singers.Length
		If Singers[i] && Singers[i].Is3DLoaded()
			Singers[i].KeepOffsetFromActor(SingerAnchor, SingerHomeX[i] + dx, SingerHomeY[i], SingerHomeZ[i], 0.0, 0.0, SingerFacing[i], SingerCatchUp, SingerFollow)
		EndIf
		i += 1
	EndWhile
	If dx != stepApplied || !stepOn
		Debug.Trace("SkyrimFairAudio: singers step to " + dx)
	EndIf
	stepApplied = dx
	stepOn = True
EndFunction

; No singer's gesture starts before a step ends.
Function HoldGestures(Float until)
	stepDone = until
	Int i = 0
	While i < singerNext.Length
		If singerNext[i] < until
			singerNext[i] = until
		EndIf
		i += 1
	EndWhile
EndFunction

; The singers let go of the anchor: their package (stay at the editor location) has them again.
Function SingersStand()
	Int i = 0
	While i < Singers.Length
		If Singers[i]
			Singers[i].ClearKeepOffsetFromActor()
		EndIf
		i += 1
	EndWhile
	stepOn = False
	stepApplied = 0.0
	stepDone = 0.0
EndFunction

; Every section whose start has come is applied, from the song's own start (so timer error
; never adds up). An instrument sent away is put away and taken up again after; a resting one
; is held still (OAR plays a held loop on the tempo global);
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
	While nextSection < endSection && nextSection < SectionStarts2.Length && SectionStarts2[nextSection] <= into + 0.05
		applied = nextSection
		crowd = SectionCrowd2[nextSection]
		nextSection += 1
	EndWhile
	If applied >= 0
		Int k = 0
		While k < count && k < 8
			Int level = SectionPlay2[applied * count + k]
			If level != playLevel[k]
				Debug.Trace("SkyrimFairAudio: instrument " + k + " level " + level + " at " + into + " s")
			EndIf
			If (level == 3) != (playLevel[k] == 3)
				If level == 3
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
		Int mode = 1
		If applied < SectionSing2.Length
			mode = SectionSing2[applied]
		EndIf
		If mode != singMode
			Debug.Trace("SkyrimFairAudio: singers " + mode + " at " + into + " s")
			; A new mode's move starts now, not when the last gesture ends.
			Int si = 0
			While si < singerNext.Length
				singerNext[si] = Utility.GetCurrentGameTime()
				si += 1
			EndWhile
		EndIf
		singMode = mode
		singing = mode > 0
	EndIf
	If crowd != crowdMode
		; Each dancer takes the new mode up as their move in hand ends (Dance), so the crowd
		; drifts into it rather than stopping as one.
		Debug.Trace("SkyrimFairAudio: crowd " + crowd + " at " + into + " s")
		crowdMode = crowd
		CapDances(Utility.GetCurrentGameTime())
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

; A dance style's clip runs up to half a minute: at a change, each dancer's move in hand is
; cut short to a few staggered seconds from now (0-3.5 s by dancer), so the floor changes
; within moments of the section, but not as one.
Function CapDances(Float now)
	Float perSecond = TimeScale.GetValue() / 86400.0
	Int i = 0
	While i < Dancers.Length && i < danceEnds.Length
		Float cap = now + (i % 8) * 0.5 * perSecond
		If danceEnds[i] > cap
			danceEnds[i] = cap
		EndIf
		i += 1
	EndWhile
EndFunction

; As a song ends: a shell from each site in turn, and more at night. They're the fair's own
; (vanilla effects): a spell cast from the site's marker at the one above it, bursting high up.
Function Fireworks()
	If !FireworksOn || FireworksOn.GetValue() < 0.5 || FireworkShells.Length == 0
		Return
	EndIf
	Int k = 0
	While k < FireworkSites.Length && k < FireworkAims.Length
		Spell shell = FireworkShells[k % FireworkShells.Length]
		If shell && FireworkSites[k] && FireworkAims[k]
			shell.Cast(FireworkSites[k], FireworkAims[k])
		EndIf
		k += 1
		If k < FireworkSites.Length
			Utility.Wait(FireworkStagger)
		EndIf
	EndWhile
	Float t = Utility.GetCurrentGameTime()
	Float hour = (t - Math.Floor(t)) * 24.0
	Int middle = FireworkSites.Length / 2
	If (hour >= 20.0 || hour < 5.0) && middle < FireworkAims.Length
		Int n = 0
		While n < FireworkNightShells.Length
			Utility.Wait(FireworkStagger)
			If FireworkNightShells[n]
				FireworkNightShells[n].Cast(FireworkSites[middle], FireworkAims[middle])
			EndIf
			n += 1
		EndWhile
	EndIf
	Debug.Trace("SkyrimFairAudio: fireworks after song " + track)
EndFunction

; Whether Professional Dancer is loaded: once a load.
Function CheckMoreDances()
	If moreDancesChecked
		Return
	EndIf
	moreDancesChecked = True
	moreDancesFound = MoreDanceEvents.Length > 0 && Game.GetFormFromFile(MoreDancesCheck, MoreDancesPlugin) != None
	Debug.Trace("SkyrimFairAudio: " + MoreDancesPlugin + " loaded: " + moreDancesFound)
EndFunction

; Whether a musician with this idle plays now: an instrument with a timeline follows it.
Bool Function Playing(Idle instrument)
	Int k = 0
	While k < InstrumentIdles.Length && k < playLevel.Length
		If InstrumentIdles[k] == instrument
			Return playLevel[k] != 3
		EndIf
		k += 1
	EndWhile
	Return True
EndFunction

; The chosen dancers turn to face the stage (its speaker), over a few quick steps. A dancer
; already facing it (within 10 degrees) stays as they are.
Function FaceStage(Bool[] which)
	If !StageSpeaker
		Return
	EndIf
	Float[] start = new Float[128]
	Float[] turn = new Float[128]
	Int i = 0
	While i < Dancers.Length && i < 128
		If which[i] && Dancers[i] && Dancers[i].Is3DLoaded()
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
	If danceMode.Length < Dancers.Length
		danceMode = new Int[128]
	EndIf
	If inMoreDance.Length < Dancers.Length
		inMoreDance = new Bool[128]
	EndIf
	CheckMoreDances()
	Float now = Utility.GetCurrentGameTime()
	Float perSecond = TimeScale.GetValue() / 86400.0
	danceWake = 0.0
	Bool[] turning = new Bool[128]
	Bool anyTurn = False
	; Pass 1: who's due, and with what: a mod dance (its event) or an idle.
	Int[] todo = new Int[128]
	Idle[] moves = new Idle[128]
	Float[] clips = new Float[128]
	String[] events = new String[128]
	Bool settle = False
	Int i = 0
	While i < Dancers.Length && i < dancing.Length
		If (!dancing[i] || now >= danceEnds[i]) && Dancers[i] && Dancers[i].Is3DLoaded()
			Int which = (i + dancePlays[i]) % idles.Length
			moves[i] = idles[which]
			clips[i] = 6.0
			If which < lengths.Length
				clips[i] = lengths[which]
			EndIf
			todo[i] = 2
			If crowdMode == 0 && moreDancesFound
				Int d = (i * 2 + dancePlays[i]) % MoreDanceEvents.Length
				events[i] = MoreDanceEvents[d]
				clips[i] = MoreDanceLengths[d]
				todo[i] = 1
			EndIf
			; A mod dance loops until stopped: settle first, into it or out of it.
			If todo[i] == 1 || inMoreDance[i]
				Debug.SendAnimationEvent(Dancers[i], "IdleForceDefaultState")
				settle = True
			EndIf
		EndIf
		i += 1
	EndWhile
	If settle
		Utility.Wait(0.1)
		now = Utility.GetCurrentGameTime()
	EndIf
	; Pass 2: start them.
	i = 0
	While i < Dancers.Length && i < dancing.Length
		Bool started = False
		If todo[i] == 1
			Debug.SendAnimationEvent(Dancers[i], events[i])
			inMoreDance[i] = True
			started = True
		ElseIf todo[i] == 2
			If Dancers[i].PlayIdle(moves[i])
				inMoreDance[i] = False
				started = True
			EndIf
		EndIf
		If started
			; Into clap or cheer from another mode: this dancer turns to the stage.
			If crowdMode > 0 && danceMode[i] != crowdMode
				turning[i] = True
				anyTurn = True
			EndIf
			danceMode[i] = crowdMode
			dancing[i] = True
			dancePlays[i] = dancePlays[i] + 1
			danceEnds[i] = now + clips[i] * perSecond
		EndIf
		If dancing[i] && (danceWake == 0.0 || danceEnds[i] < danceWake)
			danceWake = danceEnds[i]
		EndIf
		i += 1
	EndWhile
	If anyTurn
		FaceStage(turning)
	EndIf
EndFunction

; Every line whose start has come is said by all three singers. Each start is measured
; from the song's own start, so timer error never adds up from line to line.
Function Sing(Float now)
	If nextLine < 0 || Singers.Length == 0
		Return
	EndIf
	Float into = Seconds(now - songStarted)
	While nextLine < endLine && nextLine < SingerStarts2.Length && SingerStarts2[nextLine] <= into + 0.05
		Int i = 0
		While i < Singers.Length && singing
			If Singers[i] && Singers[i].Is3DLoaded()
				Singers[i].Say(SingerTopics2[nextLine])
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
	; Back to their marks for the wave (a no-op when they're on them).
	If stepOn && stepApplied != 0.0
		SingerOffset(0.0)
	EndIf
	Int s = 0
	While SingerEndMove && s < Singers.Length
		If Singers[s] && Singers[s].Is3DLoaded()
			Singers[s].PlayIdle(SingerEndMove)
		EndIf
		s += 1
	EndWhile
	; The floor cheers as each dancer's move ends (Dance runs through the cheer too).
	If CheerIdles.Length > 0
		crowdMode = 2
		CapDances(Utility.GetCurrentGameTime())
	EndIf
	nextLine = -1
	; The folk pair stops, and starts afresh with the next song.
	Int i = 0
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
