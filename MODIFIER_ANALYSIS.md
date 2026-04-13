# Creature Modifier Logic Analysis

## Modifier Test Results & Issues Found

### 1. DoubleDamage
**Trigger:** When player is CAUGHT (playerChoice == creatureChoice)
**Effect:** PlayerWillpower decreases by 1 (extra damage)

**Issues Found:**
- ⚠️ **MISSING BOUNDS CHECK**: Willpower can go negative
  - Other modifiers use `Math.Max(0, ...)` but this doesn't
  - Example: If willpower is 0 and this triggers, it becomes -1
  - **Inconsistent** with BlockPlayerProgress which should have same bounds

---

### 2. BlockPlayerProgress
**Trigger:** When player ESCAPES (playerChoice != creatureChoice)
**Effect:** PlayerProgress decreases by 1

**Issues Found:**
- ✅ Has `Math.Max(0, progress - 1)` protection
- ✅ Logically consistent - blocks progress on escape

---

### 3. LoseRandomLocation
**Trigger:** Always (no condition, just checks available locations count)
**Effect:** Removes one random available location, moves to UsedLocations

**Issues Found:**
- ⚠️ **BEHAVIOR UNCLEAR**: Triggers regardless of catch/escape
- ⚠️ **NO BOUNDS CHECK**: Always fires if locations available
- Inconsistent with other modifiers that depend on playerChoice vs creatureChoice

---

### 4. BeachAndWreckBlock ✅
**Trigger:** When player ESCAPES (playerChoice != creatureChoice) at Beach (4) or Wreck (8)
**Effect:** Cancels the location's beneficial effect

**Sub-logic:**
```
Beach (4):
  - If beacon already lit → cancel progress gain (progress--)
  - If beacon not lit → keep it false (prevent lighting)

Wreck (8):
  - Cancel progress gain (progress--)
```

**Issues Found:**
- ✅ **LOGICALLY CORRECT** - properly negates each location's benefit
- ✅ Uses `Math.Max(0, ...)` protection
- ✅ Consistent with intended design

---

### 5. ExtraCreatureProgress
**Trigger:** When player is CAUGHT (playerChoice == creatureChoice)
**Effect:** CreatureProgress increases by 1 (extra progress for creature)

**Issues Found:**
- ✅ Logically consistent with DoubleDamage (opposite player effect)
- ⚠️ **NO BOUNDS CHECK**: CreatureProgress can exceed MaxCreatureProgress
  - Although game likely checks MaxCreatureProgress elsewhere
  - Should still add protection for consistency

---

## Summary of Issues

| Modifier | Issue | Severity | Fix |
|----------|-------|----------|-----|
| DoubleDamage | Willpower can go negative | 🔴 High | Add `Math.Max(0, ...)` |
| BlockPlayerProgress | ✅ None | - | - |
| LoseRandomLocation | Triggers always (unclear) | 🟡 Medium | Add condition check |
| BeachAndWreckBlock | ✅ None | - | - |
| ExtraCreatureProgress | Progress unbounded | 🟡 Medium | Add max check |

## Recommended Fixes

### Fix 1: DoubleDamage - Add bounds check
```csharp
case CreatureModifier.DoubleDamage:
    if (playerChoice == creatureChoice)
    {
        session.PlayerWillpower = Math.Max(0, session.PlayerWillpower - 1);  // ADD THIS
        session.StatusMessage += $"\n⚠️ [Modifier: Double Damage] Существо наносит дополнительный урон!";
    }
    break;
```

### Fix 2: ExtraCreatureProgress - Add bounds check
```csharp
case CreatureModifier.ExtraCreatureProgress:
    if (playerChoice == creatureChoice)
    {
        session.CreatureProgress = Math.Min(GameSession.MaxCreatureProgress, session.CreatureProgress + 1);
        session.StatusMessage += $"\n⚠️ [Modifier: Extra Creature Progress] Существо продвигается быстрее!";
    }
    break;
```

### Fix 3: LoseRandomLocation - Clarify intent
Currently triggers always. Should this:
- Option A: Only trigger on escape? (like BlockPlayerProgress)
- Option B: Only trigger on catch? (like DoubleDamage)
- Option C: Always trigger (current, but then why not check for catch/escape)?

Current code suggests Option C, but logic is confusing.

## Consistency Analysis

**Balanced Pairs (by catch/escape):**
- DoubleDamage (on catch) ↔ BlockPlayerProgress (on escape) ✅
- ExtraCreatureProgress (on catch) ↔ BeachAndWreckBlock (on escape) ✅

**Outlier:**
- LoseRandomLocation (always active)

**Boundary Checks:**
- BeachAndWreckBlock: ✅ Uses Math.Max
- BlockPlayerProgress: ✅ Uses Math.Max
- DoubleDamage: ❌ Missing Math.Max
- ExtraCreatureProgress: ⚠️ No max bound check
- LoseRandomLocation: N/A (list manipulation)
