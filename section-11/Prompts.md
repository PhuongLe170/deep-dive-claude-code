# Claude Code Deep Dive - Section 11: Plugins Prompts

These are the prompts run on screen during this section's lessons. Copy them as they are into Claude Code and follow along.

## L4 - Creating Your Own Plugin

### Scaffold the plugin

Generates the plugin manifest along with empty skills and hooks folders, so you start from a valid structure.

```
claude plugins init cc-deep-dive-devkit --with skills hooks
```

### Load it for one session

Loads the plugin straight from the folder for this session only, so you can try changes without installing anything.

```
claude --plugin-dir ./plugins/cc-deep-dive-devkit
```

### Validate the manifest

Checks the plugin's manifest and components for errors before you publish it.

```
claude plugin validate ./plugins/cc-deep-dive-devkit
```

### Add the local marketplace

Registers the local folder as a marketplace, so the plugins inside it can be installed the same way a published one would be.

```
/plugin marketplace add ./plugins/
```
