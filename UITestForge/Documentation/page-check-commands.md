# Page Check Commands in UITestForge

UITestForge provides two complementary commands for conditional script execution based on the current page:

> **Note:** Both commands automatically refresh the visual tree to get the current page name before performing the check, ensuring the most up-to-date page information.

## checkpage
**Syntax:** `checkpage <pageName> <label>`

**Behavior:** Refreshes the visual tree, then if the current page IS the specified page, jump to the label. Otherwise, continue with the next line.

**Use Case:** Execute code only when on a specific page.

**Example:**
```
checkpage MainPage onMain
tap SomeOtherButton
exit

onMain:
tap MainPageButton
screenshot main_action.png
```

In this example:
- Refreshes the tree to get current page
- If we're on MainPage → jumps to `onMain` label → taps MainPageButton
- If we're NOT on MainPage → continues to next line → taps SomeOtherButton

---

## checknpage
**Syntax:** `checknpage <pageName> <label>`

**Behavior:** Refreshes the visual tree, then if the current page is NOT the specified page, jump to the label. Otherwise, continue with the next line.

**Use Case:** Skip sections when already on the target page, or handle being on an unexpected page.

**Example:**
```
checknpage LoginPage afterLogin
# We're on login page, perform login
tap UsernameEntry
fill UsernameEntry admin@test.com
tap PasswordEntry
fill PasswordEntry secret123
tap LoginBtn
wait 2

afterLogin:
# Continue regardless of whether we logged in
tap MainMenuBtn
```

In this example:
- Refreshes the tree to get current page
- If we're NOT on LoginPage → jumps to `afterLogin` label → skips login
- If we ARE on LoginPage → continues to next line → performs login

---

## Practical Patterns

### Pattern 1: Skip login if already logged in
```
# If we're not on login page (already logged in), skip login section
checknpage LoginPage skipLogin
call common_login.devflow

skipLogin:
tap DashboardBtn
```

### Pattern 2: Handle multiple page states
```
checkpage ErrorPage handleError
checkpage LoginPage handleLogin
# Default: we're on main page
tap ContinueBtn
goto end

handleError:
screenshot error_state.png
tap RetryBtn
goto end

handleLogin:
call common_login.devflow

end:
screenshot final_state.png
```

### Pattern 3: Conditional test execution
```
# Only run certain tests if on the right page
checknpage TestSetupPage skipTests
tap RunAllTestsBtn
wait 5
screenshot test_results.png

skipTests:
tap GoToSetupBtn
```

---

## Tips

1. **Case-Insensitive:** Page names are compared case-insensitively
2. **Label Required:** Both commands require a valid label that exists in the script
3. **Complementary:** Use `checkpage` for "if yes" logic, `checknpage` for "if no" logic
4. **Combine with goto:** Can be combined with explicit `goto` commands for complex flows
5. **Error Handling:** If the label doesn't exist, an error is logged and execution continues
6. **Auto-Refresh:** Both commands automatically refresh the visual tree before checking, so you always get the current page state
7. **Performance:** Since tree refresh happens automatically, avoid placing these commands in tight loops

---

## Related Commands

- `goto <label>` - Unconditional jump to a label
- `exit` - Stop script execution immediately
- `call <script>` - Execute another script file and return
