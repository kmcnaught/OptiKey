#Requires AutoHotkey v1.1.33+

#SingleInstance Force
#NoEnv
SetWorkingDir %A_ScriptDir%
SetBatchLines -1
#include <UIA_Interface>

; Launch Ghost 
WindowTitle := "Tobii Ghost"

EnvGet, A_LocalAppData, LocalAppData
path := A_LocalAppData . "\TobiiGhost\TobiiGhost.exe"
Run, %path%

; Waits for the TobiiGhost.exe main window to exist and be active
WinWaitActive, %WindowTitle%

UIA := UIA_Interface() ; Initialize UIA interface

ghostEl := UIA.ElementFromHandle(WindowTitle) ;

; Second tab - has a name
tabEl := ghostEl.FindFirstByNameAndType("Tobii.Ghost.ViewModels.SettingsTabViewModel", "TabItem") ; 
tabEl.Highlight()
tabEl.Click()

; MAIN ENABLED TOGGLE 
; we want the second pane, then the 1st custom, 1st button
; Unfortunately very few are named! 
panes := tabEl.FindAllByNameAndType("", "pane")
if (panes.Length() >= 2)
{
    secondPane := panes[2]
    secondPane.Highlight()
    
    secondControl := secondPane.FindFirstByType("Custom")
    secondControl.Highlight()

    ; Find a button control within the second control
    btn := secondControl.FindFirstByType("Button")
    btn.Highlight()            

	togglePattern := btn.GetCurrentPatternAs("Toggle")
	toggleState := togglePattern.CurrentToggleState
	togglePattern.ToggleState := 1
    
}

; SECONDARY PREVIEW TOGGLE
; we want the second pane, then the 3rd custom element, then a button
; Unfortunately very few are named! 
panes := tabEl.FindAllByNameAndType("", "pane")
if (panes.Length() >= 2)
{
    secondPane := panes[2]
    secondPane.Highlight()
    
    customControls := secondPane.FindAllByType("Custom")

    if (customControls.Length() >= 3)
    {
        secondControl := customControls[3]
        secondControl.Highlight()

        ; Find a button control within the second control
        btn := secondControl.FindFirstByType("Button")
        btn.Highlight()            

		togglePattern := btn.GetCurrentPatternAs("Toggle")
		toggleState := togglePattern.CurrentToggleState
		togglePattern.ToggleState := 1
    }
}

ExitApp