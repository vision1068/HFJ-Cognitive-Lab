---
name: canvas-apps-tools
description: >
  AI skill for Power Apps Canvas Apps. Generates paste-ready formulas,
  components, and patterns for canvas app development.
  Based on ToluVictor/canvas-apps-tools.
---

# Canvas Apps Tools Skill

## When to invoke
- Writing Power Fx formulas for canvas apps
- Generating reusable canvas app components
- Building gallery, form, and navigation patterns
- Optimizing canvas app performance

## Paste-ready formula patterns

### Filtered Gallery with search
```
Filter(
    'TableName',
    StartsWith(Title, SearchInput.Text) &&
    Status = StatusDropdown.Selected.Value
)
```

### Offline data sync pattern
```
// On save button:
Collect(localCollection, Form1.LastSubmit);
SaveData(localCollection, "localData");
// On app start:
LoadData(localCollection, "localData", true);
```

### Responsive container setup
```
// App.Formulas:
screenWidth = App.Width;
isMobile = App.Width < 768;
```

### Patch with error handling
```
If(
    IsError(Patch('TableName', defaults, {Field: Value})),
    Notify("Save failed. Try again.", NotificationType.Error),
    Notify("Saved successfully.", NotificationType.Success)
)
```

### Navigate with context
```
Navigate(
    ScreenName,
    ScreenTransition.Fade,
    {recordId: Gallery1.Selected.ID, mode: "edit"}
)
```

## Output format
Always produce paste-ready Power Fx. Include:
- Formula with comments on non-obvious logic
- Where to place the formula (property name, control name)
- Delegation warning if applicable
