#Requires AutoHotkey v1.1.33+

#SingleInstance Force
#NoEnv
SetWorkingDir %A_ScriptDir%
SetBatchLines -1
#include <UIA_Interface>

; Configuration
WindowTitle := "Tobii Ghost"
ActivationTimeout := 10    ; seconds to wait for window activation each attempt
MaxRetries := 10          ; maximum number of attempts
UIReadyDelay := 1000      ; milliseconds to wait before testing UIA
LogFile := A_ScriptDir . "\ghost_toggle.log"

; Clear previous log
FileDelete, %LogFile%
FileAppend, === Ghost Toggle Script Started ===`n, %LogFile%

; Launch Ghost 
EnvGet, A_LocalAppData, LocalAppData
path := A_LocalAppData . "\TobiiGhost\TobiiGhost.exe"
Run, %path%

; Wait for window to exist first
WinWait, %WindowTitle%
FileAppend, Window '%WindowTitle%' detected`n, %LogFile%

; Retry loop for activation and UIA readiness
; This is an aggressive attempt to avoid focus being stolen at the wrong time but
; also ensure we've waiting long enough for the UI to be ready
UIAReady := false 

Loop, %MaxRetries% {
    FileAppend, Attempt %A_Index%/%MaxRetries%: Trying to activate window`n, %LogFile%    
    
    ; Force activation
    WinActivate, %WindowTitle%
    WinWaitActive, %WindowTitle%, , %ActivationTimeout%
    
    OutputDebug, Window '%WindowTitle%' detected

    if (ErrorLevel) {
        FileAppend, Attempt %A_Index%: Window activation timed out after %ActivationTimeout% seconds`n, %LogFile%    
        Sleep, 500
        continue  ; Try again
    }
        
    FileAppend, % Attempt %A_Index% Window is active, testing UIA`n, %LogFile%    
    
    ; Give UI time to be ready
    Sleep, %UIReadyDelay%
    
    ; Try to initialize UIA and test if it's working
    try {
        UIA := UIA_Interface()
        ghostEl := UIA.ElementFromHandle(WindowTitle)
        
        if (ghostEl) {
            ; Test if we can find a basic element
            testEl := ghostEl.FindFirstByType("TabItem")
            if (testEl) {
                FileAppend, SUCCESS: Window active and UIA ready on attempt %A_Index%`n, %LogFile%    
                UIAReady := true                
                break  ; Success! Exit the loop
            }
        }
        
        FileAppend, Attempt %A_Index%: UIA initialized but elements not ready`n, %LogFile%    
    } catch e {
        errorMsg := e.message
        FileAppend, Attempt %A_Index%: UIA initialization failed - %errorMsg%`n, %LogFile%
    }
        
    ; If we get here, UIA wasn't ready - try again
    Sleep, 5000  ; Brief pause before retry
}

; Check if we succeeded or exhausted retries
if (!UIAReady) {
    FileAppend, ERROR: Failed to get window active with working UIA after %MaxRetries% attempts`n, %LogFile%    
    MsgBox, 16, Ghost Toggle Error, ERROR: Failed to get window active with working UIA after %MaxRetries% attempts.`n`nCheck the log file for details:`n%LogFile%
    ; (Exiting now allows rest of exhibit to continue, but forces someone to acknowledge error)
    ExitApp
}

; Now it's safe to get on with automation
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