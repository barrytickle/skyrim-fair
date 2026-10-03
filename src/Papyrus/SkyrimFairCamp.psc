Scriptname SkyrimFairCamp Extends Quest
{2.0.2, the fair comes and goes (FairCamp.cs, docs/CAMP.md). Every few seconds: should the fair be
here? If that has changed, the fair and Claudius's camp swap, but only out of the player's sight:
never with the player in the fair's worldspace, and in Tamriel only beyond SwapDistance of the
site. Everything outside in Tamriel follows Present (its enable parent); the camp and the vanilla
scenery the fair clears follow it the opposite way. Claudius's lines (SkyrimFairCampLine) call Said.
Choosing a schedule with him swaps at once (Barry, 2026-10-04): the screen fades to black, the fair
goes, and the player is put before the camp, with Claudius, Garrick at his lute and the roof horse
on its boulder; or the reverse, back before the gate.}

GlobalVariable Property Away Auto
{0 the fair is here, 1 it's away and the camp is there. His packages and lines read it.}
GlobalVariable Property Schedule Auto
{0 always (pass 2: 1 festival days, 2 fair week, 3 wandering).}
GlobalVariable Property StoryGate Auto
{1: the fair only after the main story (MainQuest completed).}
GlobalVariable Property StoryDone Auto
{1 once he's remarked on the story's end at the fair.}
ObjectReference Property Present Auto
ObjectReference Property CampMarker Auto
ObjectReference Property ViewMarker Auto
{Where the player stands when he brings the fair back, facing the gate.}
ObjectReference Property EntranceMarker Auto
{Inside the fair, where he waits for arrivals.}
Actor Property Inspector Auto
WorldSpace Property FairWorld Auto
WorldSpace Property Tamriel Auto
Quest Property MainQuest Auto
ImageSpaceModifier Property FadeOut Auto
ImageSpaceModifier Property FadeHold Auto
ImageSpaceModifier Property FadeBack Auto
Float Property SwapDistance = 12000.0 Auto
Float Property Poll = 5.0 Auto

ObjectReference Property ArriveMarker Auto
{Where the player is put when a schedule sends the fair away: facing the camp.}
Actor Property Bard Auto
{Garrick: at the camp he plays his lute by the fire.}
ObjectReference Property BardSpot Auto
Idle Property Lute Auto
Float Property LuteEvery = 300.0 Auto
Actor Property Horse Auto
{The roof horse: on its boulder at the camp (SkyrimFairRoofHorse keeps it there).}
ObjectReference Property HorseSpot Auto

Bool busy = False
Bool bringing = False
; A schedule's swap, waiting for his line to end: 0 none, 1 the fair back, 2 the fair away.
Int moving = 0
Bool waitLogged = False
Float luteUntil = 0.0

Event OnInit()
	Debug.Trace("SkyrimFairCamp: started, the fair is " + Where())
	RegisterForSingleUpdate(Poll)
EndEvent

Event OnUpdate()
	If bringing
		; Pass 1's flag, on a save from that build: the fair back.
		bringing = False
		moving = 1
	EndIf
	If moving != 0
		; Once his line is over, the fade.
		If Inspector && Inspector.IsInDialogueWithPlayer()
			RegisterForSingleUpdate(0.5)
			Return
		EndIf
		Int m = moving
		moving = 0
		FadeSwap(m == 1)
	Else
		Check()
	EndIf
	PlayLute()
	RegisterForSingleUpdate(Poll)
EndEvent

; Should the fair be here?
Bool Function Wanted()
	If StoryGate.GetValue() >= 0.5 && !MainQuest.IsCompleted()
		Return False
	EndIf
	Return True
EndFunction

; Left to itself (the main story finished, a console change), the fair comes or goes out of sight.
Function Check()
	If busy
		Return
	EndIf
	Bool here = Away.GetValue() < 0.5
	Bool want = Wanted()
	If want == here
		waitLogged = False
		Return
	EndIf
	If !OutOfSight()
		If !waitLogged
			waitLogged = True
			Debug.Trace("SkyrimFairCamp: the fair should be " + StateWord(want) + ", waiting until the site is out of sight")
		EndIf
		Return
	EndIf
	; Marked busy before anything that waits, so a fade can't start a swap meanwhile.
	busy = True
	Swap(want)
	busy = False
EndFunction

; The player can't see the site: in another worldspace or an interior (no worldspace), or in
; Tamriel far enough away. Never in the fair itself.
Bool Function OutOfSight()
	Actor player = Game.GetPlayer()
	WorldSpace ws = player.GetWorldSpace()
	If ws == FairWorld
		Return False
	ElseIf ws != Tamriel
		Return True
	EndIf
	Return player.GetDistance(CampMarker) > SwapDistance
EndFunction

; The fair here (True) or away (False), and the camp's three with it. The caller marks itself busy.
Function Swap(Bool here)
	If here
		Away.SetValue(0.0)
		Present.Enable()
		PlaceAt(Inspector, EntranceMarker)
		PlaceAt(Bard, None)
		PlaceHorse(None)
	Else
		Away.SetValue(1.0)
		Present.Disable()
		PlaceAt(Inspector, CampMarker)
		PlaceAt(Bard, BardSpot)
		PlaceHorse(HorseSpot)
		luteUntil = 0.0
	EndIf
	Debug.Trace("SkyrimFairCamp: the fair is " + Where())
EndFunction

; An actor to a marker, or (None) back to where the plugin placed him.
Function PlaceAt(Actor a, ObjectReference where)
	If !a || a.IsDead()
		Return
	EndIf
	If a.GetSitState() >= 2 || a.Is3DLoaded()
		Debug.SendAnimationEvent(a, "IdleForceDefaultState")
	EndIf
	If where
		a.MoveTo(where)
	Else
		a.MoveToMyEditorLocation()
	EndIf
	a.EvaluatePackage()
EndFunction

; The horse onto its boulder (its AI off, so nothing walks it off), or back to its roof.
Function PlaceHorse(ObjectReference where)
	If !Horse
		Return
	EndIf
	Horse.SetDontMove(False)
	If where
		Horse.MoveTo(where)
		Horse.EnableAI(False)
	Else
		Horse.MoveToMyEditorLocation()
	EndIf
	Horse.SetDontMove(True)
EndFunction

; Garrick's lute at the camp, while he's loaded and free; replayed every LuteEvery seconds, and
; again after a talk.
Function PlayLute()
	If !Bard || !Lute || Away.GetValue() < 0.5
		Return
	EndIf
	If !Bard.Is3DLoaded() || Bard.IsInDialogueWithPlayer() || Bard.IsInCombat() || Bard.GetSitState() != 0
		luteUntil = 0.0
		Return
	EndIf
	Float now = Utility.GetCurrentRealTime()
	; (Real time starts again at each launch: a deadline further off than LuteEvery is a past launch's.)
	If now < luteUntil && luteUntil - now <= LuteEvery
		Return
	EndIf
	If Bard.PlayIdle(Lute)
		luteUntil = now + LuteEvery
	Else
		luteUntil = now + 5.0
	EndIf
EndFunction

; ---- his lines (SkyrimFairCampLine) --------------------------------------------------------
; 1 bring it back, 2 the story skipped and bring it back, 3 every day, 4 only after the story,
; 5 the story's end remarked on.
Function Said(Int step)
	Debug.Trace("SkyrimFairCamp: line, step " + step + ", the fair is " + Where())
	If step == 1 || step == 2
		If step == 2
			StoryGate.SetValue(0.0)
		EndIf
		moving = 1
	ElseIf step == 3
		StoryGate.SetValue(0.0)
		Schedule.SetValue(0.0)
		If Away.GetValue() >= 0.5
			moving = 1
		EndIf
	ElseIf step == 4
		StoryGate.SetValue(1.0)
		StoryDone.SetValue(0.0)
		If Away.GetValue() < 0.5 && !Wanted()
			moving = 2
		EndIf
	ElseIf step == 5
		StoryDone.SetValue(1.0)
	EndIf
	If moving != 0
		RegisterForSingleUpdate(0.5)
	EndIf
EndFunction

; Fade to black, the swap, the player put before the gate (here) or the camp (away), fade in.
Function FadeSwap(Bool here)
	If busy || (Away.GetValue() < 0.5) == here
		Return
	EndIf
	busy = True
	Debug.Trace("SkyrimFairCamp: fading, the fair to be " + StateWord(here))
	Game.DisablePlayerControls(True, True, False, False, True, False, False)
	FadeOut.Apply()
	Utility.Wait(2.0)
	FadeOut.PopTo(FadeHold)
	Swap(here)
	If here
		Game.GetPlayer().MoveTo(ViewMarker)
	Else
		Game.GetPlayer().MoveTo(ArriveMarker)
	EndIf
	Utility.Wait(2.0)
	If !here
		PlayLute()
	EndIf
	FadeHold.PopTo(FadeBack)
	Game.EnablePlayerControls(True, True, False, False, True, False, False)
	busy = False
EndFunction

String Function StateWord(Bool here)
	If here
		Return "here"
	EndIf
	Return "away"
EndFunction

String Function Where()
	If Away.GetValue() >= 0.5
		Return "away (the camp)"
	EndIf
	Return "here"
EndFunction
