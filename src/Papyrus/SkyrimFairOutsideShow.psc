Scriptname SkyrimFairOutsideShow extends ObjectReference
{The Wanderer's Fair from outside its Tamriel walls: the fair's songs, faintly, one after another,
and at night a volley of fireworks over it now and then. Runs only while its cell is attached.}

Sound[] Property Songs Auto
Float[] Property SongLengths Auto
Float Property SongGap = 20.0 Auto
ObjectReference Property Speaker Auto

ObjectReference[] Property FireworkSites Auto
ObjectReference[] Property FireworkAims Auto
Spell[] Property FireworkShells Auto
Float Property FireworkStagger = 0.6 Auto
Float Property FireworkEvery = 150.0 Auto

GlobalVariable Property FireworksOn Auto
GlobalVariable Property MusicEnabled Auto

Int song = 0
Int instance = 0
Float songEnds = 0.0
Float nextVolley = 0.0
Int volley = 0
Bool running = False

Event OnCellAttach()
	Begin()
EndEvent

Event OnLoad()
	Begin()
EndEvent

Event OnCellDetach()
	Halt()
EndEvent

Event OnUnload()
	Halt()
EndEvent

Function Begin()
	If running
		Return
	EndIf
	running = True
	songEnds = 0.0
	nextVolley = Utility.GetCurrentRealTime() + 15.0
	RegisterForSingleUpdate(1.0)
EndFunction

Function Halt()
	running = False
	UnregisterForUpdate()
	If instance
		Sound.StopInstance(instance)
		instance = 0
	EndIf
EndFunction

Event OnUpdate()
	If !running
		Return
	EndIf
	Float now = Utility.GetCurrentRealTime()
	If Songs.Length > 0 && now >= songEnds && (!MusicEnabled || MusicEnabled.GetValue() > 0.5)
		If instance
			Sound.StopInstance(instance)
		EndIf
		instance = Songs[song].Play(Speaker)
		songEnds = now + SongLengths[song] + SongGap
		song = (song + 1) % Songs.Length
	EndIf
	If now >= nextVolley
		nextVolley = now + FireworkEvery * Utility.RandomFloat(0.8, 1.3)
		If IsNight()
			Volley()
		EndIf
	EndIf
	If running
		RegisterForSingleUpdate(2.0)
	EndIf
EndEvent

Bool Function IsNight()
	Float t = Utility.GetCurrentGameTime()
	Float hour = (t - Math.Floor(t)) * 24.0
	Return hour >= 20.0 || hour < 5.0
EndFunction

; One shell from each site in turn, each a different colour from the last volley's.
Function Volley()
	If (FireworksOn && FireworksOn.GetValue() < 0.5) || FireworkShells.Length == 0
		Return
	EndIf
	Int k = 0
	While k < FireworkSites.Length && k < FireworkAims.Length
		Spell shell = FireworkShells[(volley + k) % FireworkShells.Length]
		If shell && FireworkSites[k] && FireworkAims[k]
			shell.Cast(FireworkSites[k], FireworkAims[k])
		EndIf
		k += 1
		If k < FireworkSites.Length
			Utility.Wait(FireworkStagger)
		EndIf
	EndWhile
	volley += 1
EndFunction
