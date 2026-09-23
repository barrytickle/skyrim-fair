Scriptname SkyrimFairNpcGuard extends Actor
{On every fair NPC. As each one loads, it takes off the spells in StripSpells: other
mods' abilities that, at the fair's density, flood Papyrus (a detection cloak on every
NPC, a script that runs on every effect applied). The stage quest fills the list from
the plugins that are loaded, so the fair needs none of them as masters.}

FormList Property StripSpells Auto

Int retries = 0

Event OnLoad()
	retries = 0
	Strip()
EndEvent

; A save loaded at the fair may bring NPCs in without an OnLoad.
Event OnCellAttach()
	Strip()
EndEvent

; After a load the NPCs can load before the stage quest has filled the list: try again a
; few times, 5 s apart.
Event OnUpdate()
	Strip()
EndEvent

; SPID puts these spells on the NPC's record, which vanilla RemoveSpell doesn't reach, so
; Papyrus Extender's RemoveBaseSpell takes them off the record (and the actor). Removing
; from the record lasts until the game restarts, when SPID adds them again.
Function Strip()
	If !StripSpells
		Return
	EndIf
	If StripSpells.GetSize() == 0
		If retries < 3
			retries += 1
			RegisterForSingleUpdate(5.0)
		EndIf
		Return
	EndIf
	Int i = StripSpells.GetSize()
	While i > 0
		i -= 1
		Spell s = StripSpells.GetAt(i) as Spell
		If s
			Bool fromBase = PO3_SKSEFunctions.RemoveBaseSpell(self, s)
			Bool added = RemoveSpell(s)
			If fromBase || added
				Debug.Trace("SkyrimFairGuard: " + self + " " + s + " removed from the record " + fromBase + ", from the actor " + added + ", still has it " + HasSpell(s))
			EndIf
		EndIf
	EndWhile
EndFunction
