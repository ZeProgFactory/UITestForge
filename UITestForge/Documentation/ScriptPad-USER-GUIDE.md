# ScriptPad – User Guide

*Where scripts and markdown come alive.*

This guide describes the ScriptPad editor as seen by the end user. Applications may hide, rename or
extend parts of it, so some elements described here may not be present in every app.

---

## 1. The editor at a glance

| Area | What it does |
|------|--------------|
| **File explorer** (left) | Browse the working folder and open a file with a single tap. |
| **Toolbar** (top) | Undo/Redo, clipboard, find/replace, load/save, word wrap and line numbers. |
| **Status bar** | Current caret position (`Ln`, `Col`) and a *modified* indicator. |
| **Editing surface** | The text itself, with line numbers, current‑line highlight and syntax colouring. |

An orange marker in the status bar means the file has unsaved changes. It disappears after saving.

---

## 2. Toolbar buttons

| Icon | Command | What it does |
|------|---------|--------------|
| ↶ | Undo | Reverts the last change. |
| ↷ | Redo | Re‑applies a change that was undone. |
| 📋 | Copy | Copies the selection to the clipboard. |
| ✂ | Cut | Copies the selection and removes it. |
| 📄 | Paste | Inserts the clipboard content at the caret. |
| 🔍 | Find | Opens the search dialog. |
| 🔄 | Replace All | Opens the search & replace dialog. |
| 📁 | Load File | Opens a file into the editor. |
| 💾 | Save File | Saves the current text to a file. |

Two check boxes are placed at the end of the toolbar:

- **Word Wrap** – wraps long lines to the editor width instead of scrolling horizontally.
- **Line Numbers** – shows or hides the number gutter on the left of the text.

---

## 3. Working with files

### Opening a file

- Tap a file in the **file explorer**, or
- press the **📁 Load File** button and pick a file.

Folders in the explorer are expanded and collapsed by tapping their arrow. The explorer can be
configured by the application to show only certain file types (for example only `*.md`).

### Saving

Press **💾 Save File**. Depending on the application, the file is either written back to where it was
loaded from, or a save dialog asks for a location and a name.

> **Tip:** the modified marker in the status bar tells you whether there is anything to save.

### File context menu

Long‑press (or right‑click) a file in the explorer to get file operations such as **Rename**,
**Duplicate** and **Delete**. Applications may add one extra, application specific entry.
Deleting a file cannot be undone from the editor.

---

## 4. Typing and moving around

- Tap (or click) to place the caret.
- Drag to select text; on desktop you can also hold <kbd>Shift</kbd> and use the arrow keys.
- <kbd>Tab</kbd> inserts spaces. With a selection, <kbd>Tab</kbd> indents all selected lines and
  <kbd>Shift</kbd>+<kbd>Tab</kbd> removes one level of indentation.
- Pressing <kbd>Enter</kbd> keeps the indentation of the previous line.
- The line the caret is on is highlighted so it is easy to find again.

On phones and tablets the soft keyboard opens as soon as you tap into the text.

---

## 5. Keyboard shortcuts (desktop)

| Shortcut | Action |
|----------|--------|
| <kbd>Ctrl</kbd>+<kbd>A</kbd> | Select all |
| <kbd>Ctrl</kbd>+<kbd>C</kbd> / <kbd>X</kbd> / <kbd>V</kbd> | Copy / Cut / Paste |
| <kbd>Ctrl</kbd>+<kbd>Z</kbd> | Undo |
| <kbd>Ctrl</kbd>+<kbd>Y</kbd> or <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Z</kbd> | Redo |
| <kbd>Ctrl</kbd>+<kbd>D</kbd> | Duplicate the current line |
| <kbd>Ctrl</kbd>+<kbd>L</kbd> | Delete the current line |
| <kbd>Ctrl</kbd>+<kbd>F</kbd> | Find |
| <kbd>Ctrl</kbd>+<kbd>H</kbd> | Find and replace |
| <kbd>F3</kbd> / <kbd>Shift</kbd>+<kbd>F3</kbd> | Find next / previous |
| <kbd>Esc</kbd> | Clear the selection |
| <kbd>Ctrl</kbd>+<kbd>←</kbd> / <kbd>→</kbd> | Move one word left / right |
| <kbd>Home</kbd> / <kbd>End</kbd> | Start / end of line |
| <kbd>Ctrl</kbd>+<kbd>Home</kbd> / <kbd>End</kbd> | Start / end of document |
| <kbd>Page ↑</kbd> / <kbd>Page ↓</kbd> | Scroll one screen |
| <kbd>Ctrl</kbd>+<kbd>Backspace</kbd> / <kbd>Delete</kbd> | Delete the previous / next word |

Hold <kbd>Shift</kbd> with any navigation key to extend the selection.

---

## 6. Find and replace

1. Press <kbd>Ctrl</kbd>+<kbd>F</kbd> (or 🔍) to search, <kbd>Ctrl</kbd>+<kbd>H</kbd> (or 🔄) to
   search and replace.
2. Type the search text. All matches are highlighted in the document.
3. Options:
   - **Match case** – distinguishes upper and lower case.
   - **Whole word** – only matches complete words.
   - **Regular expression** – interprets the search text as a .NET regular expression.
4. Use *Next* / *Previous* (or <kbd>F3</kbd> / <kbd>Shift</kbd>+<kbd>F3</kbd>) to step through the
   matches, *Replace* for the current match, or *Replace All* for the whole document.

Every replacement can be undone with <kbd>Ctrl</kbd>+<kbd>Z</kbd>.

---

## 7. Context menu in the text

Long‑press (or right‑click) inside the text to get the editing menu: **Cut**, **Copy**, **Paste**,
**Select All**, **Find**, **Replace**, and the formatting helpers **Bold**, **Italic** and
**Inline code**, which wrap the selected text with the corresponding markdown markers.

---

## 8. Syntax highlighting and themes

ScriptPad colours the text as you type. With markdown highlighting you will see headings, emphasis,
inline code and fenced code blocks, block quotes, list markers, horizontal rules and links in
distinct colours.

The application decides which language highlighting is active and whether a light or a dark colour
theme is used; both are usually tied to the system appearance.

---

## 9. Frequently asked questions

**Long lines run off the screen.**
Enable the **Word Wrap** check box in the toolbar.

**Can I hide the line numbers?**
Yes – clear the **Line Numbers** check box.

**I deleted text by mistake.**
Press <kbd>Ctrl</kbd>+<kbd>Z</kbd> (or ↶) as often as needed; ↷ redoes.

**The editor does not react to typing (desktop).**
Click once inside the text so the editor gets the keyboard focus.

**How do I know whether my changes are saved?**
The modified marker next to the caret position is shown only while there are unsaved changes.
