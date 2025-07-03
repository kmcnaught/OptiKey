Each instruction is demarked by '---'. Please execute these instructions in order, but stop at the end of each instruction for review. Don't start the next task until I've asked you to continue. Feel free to make notes in this file for housekeeping. 

---

The app uses Settings such as ExhibitNextButtonColor (and Back, Reset, Info) to define the colours of button icons and text. Can you change the default values to these:
    

Next,  "#19ac36"
Back,  "#3e9dcc"
info, "#cd0915"
reset, "Black"

---

The buttons are defined in JuliusSweetland.OptiKey.UI.Controls.ExhibitButton. They have a parameterised colour and label. I also want there to be an icon in the middle of the button. This will be defined by text (an ascii icon). Please add this functionality and use:
- an ascii right arrow for "next"
- left arrow for "prev"
- a "reset" circle (like an anticlockwise circular arrow) for "reset"
- a "i" in a circle for "info"

---

Inside CreateButtonGraphics we draw the button. I'm worried that people could set an unhelpful colour, e.g. a white button would not show up. All these labels have a white background. Is it possible to set a minimum contrast (3:1 or so) and scale up the darkness of the colour until it meets the threshold? So white would become light grey (as dark as required for visibility) and light yellow would become dark yellow etc.

COMPLETED: Created ContrastEnsureConverter that automatically darkens colors to ensure 3:1 contrast ratio against white background. Applied to button template so all button colors are automatically adjusted if needed. 
