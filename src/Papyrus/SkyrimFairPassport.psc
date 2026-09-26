Scriptname SkyrimFairPassport Extends Quest
{The Fair Passport (FairPassport.cs, fairWorld.passport): Claudius Vale hands it out, its
objectives are the stamps, and a full card handed back to him earns the Seal and the deed.

Stages: 0 not issued; 10 issued, collecting; 15 every stamp, return it; 20 returned (complete).
Objectives: 10 a whole song, 20 Garrick, 40 the chats, 50 the roof horse, 60 the return (30,
Claudius's own stamp, is retired: he hands the passport over, so it was done at once). The stamps come from the stage script (SongStarted, SongEnded), the cameos' lines
(SkyrimFairCameoLine: Stamp), the talk perk (SkyrimFairTalkDuck: Chat), and this script's own
check for the horse while a card is being collected.}

Book Property PassportNote Auto
{The passport itself (a small journal).}
ReferenceAlias Property PassportItem Auto
{Holds the player's passport while the card is collected: a Quest Object, so it can't be dropped,
sold or stored.}
Book Property HorseDeed Auto
Armor Property Seal Auto
{Claudius's Seal of Approval.}
GlobalVariable Property Chats Auto
{Visitors talked to since the passport was issued (shown in the objective).}
Int Property ChatsNeeded Auto
FormList Property Chatted Auto
{The visitors already counted, so each counts once.}
Actor Property Horse Auto
{The roof horse.}
Float Property HorseRange Auto
WorldSpace Property Fair Auto

Bool songWatching = False

; ---- the lines (SkyrimFairPassportLine): 1 the hand-over, 2 the hand-in -------------------
Function Hand(Int step)
	If step == 1 && GetStage() < 10
		GiveCard(False)
		SetStage(10)
		SetObjectiveDisplayed(10)
		SetObjectiveDisplayed(20)
		SetObjectiveDisplayed(40)
		SetObjectiveDisplayed(50)
		RegisterForSingleUpdate(2.0)
	ElseIf step == 2 && GetStage() == 15
		Actor player = Game.GetPlayer()
		; He takes the passport back.
		ObjectReference card = PassportItem.GetReference()
		PassportItem.Clear()
		If card && player.GetItemCount(card) > 0
			player.RemoveItem(card, 1)
		ElseIf player.GetItemCount(PassportNote) > 0
			player.RemoveItem(PassportNote, 1)
		EndIf
		player.AddItem(Seal, 1)
		player.AddItem(HorseDeed, 1)
		SetObjectiveCompleted(60)
		SetStage(20)
	EndIf
EndFunction

; ---- the passport as a reference of its own, held by the Quest Object alias -------------------
Function GiveCard(Bool silent)
	Actor player = Game.GetPlayer()
	ObjectReference card = player.PlaceAtMe(PassportNote, 1, True, True)
	PassportItem.ForceRefTo(card)
	player.AddItem(card, 1, silent)
EndFunction

; ---- a stamp: its objective ticked; with all four, the return is shown ----------------------
Function Stamp(Int objective)
	If GetStage() != 10 || objective == 30 || IsObjectiveCompleted(objective)
		Return
	EndIf
	SetObjectiveCompleted(objective)
	If !IsObjectiveCompleted(10) || !IsObjectiveCompleted(20) || !IsObjectiveCompleted(40) || !IsObjectiveCompleted(50)
		Return
	EndIf
	SetStage(15)
	SetObjectiveDisplayed(60)
EndFunction

; ---- a visitor talked to (the talk perk) ----------------------------------------------------
Function Chat(ObjectReference who)
	If GetStage() != 10 || !who || IsObjectiveCompleted(40) || Chatted.HasForm(who)
		Return
	EndIf
	Chatted.AddForm(who)
	Chats.Mod(1.0)
	UpdateCurrentInstanceGlobal(Chats)
	If Chats.GetValue() >= ChatsNeeded
		Stamp(40)
	Else
		SetObjectiveDisplayed(40, True, True)
	EndIf
EndFunction

; ---- a whole song: the player at the fair from its first note to its last ---------------------
Function SongStarted()
	songWatching = GetStage() == 10 && Game.GetPlayer().GetWorldSpace() == Fair
EndFunction

Function SongEnded()
	If songWatching && Game.GetPlayer().GetWorldSpace() == Fair
		Stamp(10)
	EndIf
	songWatching = False
EndFunction

; ---- while collecting: the horse in sight, and leaving the fair ends a song watched ---------
Event OnUpdate()
	If GetStage() != 10
		Return
	EndIf
	Actor player = Game.GetPlayer()
	; A passport handed over before it was a quest item (a 2.0.1.3 test build) becomes one.
	If !PassportItem.GetReference() && player.GetItemCount(PassportNote) > 0
		player.RemoveItem(PassportNote, 1, True)
		GiveCard(True)
	EndIf
	If player.GetWorldSpace() != Fair
		songWatching = False
	ElseIf Horse && !IsObjectiveCompleted(50) && player.GetDistance(Horse) < HorseRange && player.HasLOS(Horse)
		Stamp(50)
	EndIf
	If GetStage() == 10
		RegisterForSingleUpdate(2.0)
	EndIf
EndEvent
