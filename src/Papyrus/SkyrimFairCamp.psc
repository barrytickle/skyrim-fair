Scriptname SkyrimFairCamp Extends Quest
{2.0.2, the fair comes and goes (FairCamp.cs, docs/CAMP.md). Every few seconds: should the fair be
here? If that has changed, the fair and Claudius's camp swap, but only out of the player's sight:
never with the player in the fair's worldspace, and in Tamriel only beyond SwapDistance of the
site. Everything outside in Tamriel follows Present (its enable parent); the camp and the vanilla
scenery the fair clears follow it the opposite way. Claudius's lines (SkyrimFairCampLine) call Said.}

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

Bool busy = False
Bool bringing = False

Event OnInit()
	Debug.Trace("SkyrimFairCamp: started, the fair is " + Where())
	RegisterForSingleUpdate(Poll)
EndEvent

Event OnUpdate()
	If bringing
		; Brought back at the camp: once his line is over, the fade.
		If Inspector && Inspector.IsInDialogueWithPlayer()
			RegisterForSingleUpdate(0.5)
			Return
		EndIf
		BringNow()
	Else
		Check()
	EndIf
	RegisterForSingleUpdate(Poll)
EndEvent

; Should the fair be here?
Bool Function Wanted()
	If StoryGate.GetValue() >= 0.5 && !MainQuest.IsCompleted()
		Return False
	EndIf
	Return True
EndFunction

Function Check()
	If busy
		Return
	EndIf
	Bool here = Away.GetValue() < 0.5
	Bool want = Wanted()
	If want == here || !OutOfSight()
		Return
	EndIf
	; Marked busy before anything that waits, so the camp's fade can't start a swap meanwhile.
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

; The fair here (True) or away (False), and Claudius with it. The caller marks itself busy.
Function Swap(Bool here)
	If here
		Away.SetValue(0.0)
		Present.Enable()
		PlaceAt(EntranceMarker)
	Else
		Away.SetValue(1.0)
		Present.Disable()
		PlaceAt(CampMarker)
	EndIf
	Debug.Trace("SkyrimFairCamp: the fair is " + Where())
EndFunction

Function PlaceAt(ObjectReference where)
	Actor a = Inspector
	If !a || a.IsDead() || !where
		Return
	EndIf
	If a.GetSitState() >= 2
		Debug.SendAnimationEvent(a, "IdleForceDefaultState")
	EndIf
	a.MoveTo(where)
	a.EvaluatePackage()
EndFunction

; ---- his lines (SkyrimFairCampLine) --------------------------------------------------------
; 1 bring it back, 2 the story skipped and bring it back, 3 every day, 4 only after the story,
; 5 the story's end remarked on.
Function Said(Int step)
	Debug.Trace("SkyrimFairCamp: line, step " + step)
	If step == 1 || step == 2
		If step == 2
			StoryGate.SetValue(0.0)
		EndIf
		bringing = True
		RegisterForSingleUpdate(0.5)
	ElseIf step == 3
		StoryGate.SetValue(0.0)
		Schedule.SetValue(0.0)
	ElseIf step == 4
		StoryGate.SetValue(1.0)
		StoryDone.SetValue(0.0)
	ElseIf step == 5
		StoryDone.SetValue(1.0)
	EndIf
EndFunction

; Fade to black, the fair back, the player before its gate, fade in.
Function BringNow()
	bringing = False
	If busy || Away.GetValue() < 0.5
		Return
	EndIf
	busy = True
	Game.DisablePlayerControls(True, True, False, False, True, False, False)
	FadeOut.Apply()
	Utility.Wait(2.0)
	FadeOut.PopTo(FadeHold)
	Swap(True)
	Game.GetPlayer().MoveTo(ViewMarker)
	Utility.Wait(1.5)
	FadeHold.PopTo(FadeBack)
	Game.EnablePlayerControls(True, True, False, False, True, False, False)
	busy = False
EndFunction

String Function Where()
	If Away.GetValue() >= 0.5
		Return "away (the camp)"
	EndIf
	Return "here"
EndFunction
