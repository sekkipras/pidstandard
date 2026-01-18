# Quick Reference Card - New Features

## 🚀 Quick Access Guide

### AutoCAD Commands

| Command | Description | Usage |
|---------|-------------|-------|
| `PIDSYNC` | Bidirectional synchronization | Interactive sync between drawing and database |

**PIDSYNC Options:**
- Option 1: Full Sync (Drawing ↔ Database)
- Option 2: Drawing → Database only
- Option 3: Database → Drawing only

---

### WPF Application Features

#### 📋 Menu Locations

**Equipment Menu:**
- Equipment → **Tag Renumbering Wizard** - Bulk renumber equipment tags
- Equipment → **Hierarchical View** - Explore equipment relationships

**Main Tabs:**
- **Audit Log** tab - View and filter change history

---

## 🔄 Bidirectional Sync (PIDSYNC)

### When to Use
- After tagging equipment in AutoCAD
- After updating equipment in WPF database
- To ensure data consistency between drawing and database

### Quick Steps
```
1. In AutoCAD: PIDSYNC
2. Select sync mode
3. Review changes
4. Confirm
```

### Smart Features
✓ Detects new equipment
✓ Updates existing equipment
✓ Tracks modification dates
✓ Shows detailed statistics

---

## 📜 Audit Trail

### Access
WPF App → **Audit Log** tab

### Filters Available
- **Entity Type:** Equipment, Line, Instrument, Drawing, Project
- **Action:** Created, Updated, Deleted, Batch Tagged, Synchronized
- **Time Range:** 24 Hours, 7 Days, 30 Days, All Time

### Information Tracked
- Who made the change
- When it was made
- What was changed
- Old vs new values (JSON)
- Source (machine/IP)

### Quick Steps
```
1. Select Audit Log tab
2. Choose project
3. Apply filters
4. Click Refresh
```

---

## 🔢 Tag Renumbering Wizard

### Access
Equipment menu → **Tag Renumbering Wizard**

### Pattern Placeholders
| Placeholder | Description | Example |
|-------------|-------------|---------|
| `{TYPE}` | Equipment type code | PMP, TK, VLV |
| `{AREA}` | Area code | A01, B02 |
| `{SEQ:###}` | Sequence number | 001, 0001 |

### Pattern Examples
- `P-{SEQ:001}` → P-001, P-002, P-003
- `{AREA}-{TYPE}-{SEQ:000}` → A01-PMP-001, A01-PMP-002
- `={TYPE}{AREA}-{SEQ:0000}` → =PMPA01-0001 (KKS style)

### Quick Steps
```
1. Equipment → Tag Renumbering Wizard
2. Filter equipment (optional)
3. Define pattern
4. Preview changes
5. Apply renumbering
```

### Features
✓ Live preview
✓ Duplicate detection
✓ Conflict checking
✓ Transaction-based updates
✓ Audit logging

---

## 🌳 Hierarchical View

### Access
Equipment menu → **Hierarchical View**

### View Modes

#### 1️⃣ Group by Area
Shows equipment organized by location
```
📍 Area A01
  ⚙ Pumps (5)
    • P-001
    • P-002
  ⚙ Tanks (3)
    • TK-001
```

#### 2️⃣ Group by Equipment Type
Shows all equipment of same type together
```
⚙ Pumps (15)
  • P-001
  • P-002
⚙ Tanks (8)
  • TK-001
```

#### 3️⃣ Process Flow (Connections)
Shows upstream/downstream relationships
```
🔄 Process Flow
  🔧 P-001
    🛢 TK-001
      ⚙ P-002
```

#### 4️⃣ Group by Drawing
Shows equipment by source drawing
```
📄 DWG-001
  🔧 P-001
  🛢 TK-001
📄 DWG-002
  ⚙ P-002
```

### Details Panel Tabs
1. **Equipment Details** - Full specifications
2. **Connected Lines** - Incoming/outgoing lines
3. **Connected Equipment** - Upstream/downstream
4. **Instruments** - Associated instruments
5. **Process Parameters** - Pressure, temp, flow, capacity

### Quick Steps
```
1. Equipment → Hierarchical View
2. Select view mode
3. Expand tree nodes
4. Click equipment to view details
```

---

## 🎯 Common Workflows

### Workflow 1: New Drawing Import
```
AutoCAD:
1. PIDEXTRACT → Extract equipment
2. PIDTAG → Tag equipment
3. PIDSYNC → Sync to database

WPF:
4. Review in Equipment tab
5. Check Audit Log for imports
```

### Workflow 2: Tag Standardization
```
WPF:
1. Equipment → Tag Renumbering Wizard
2. Filter equipment by type/area
3. Define pattern (e.g., {TYPE}-{SEQ:001})
4. Preview and apply

AutoCAD:
5. PIDSYNC → Update drawings
```

### Workflow 3: Change Tracking
```
WPF:
1. Make changes to equipment
2. Check Audit Log tab
3. Filter by time range
4. Review who changed what
```

### Workflow 4: Relationship Analysis
```
WPF:
1. Equipment → Hierarchical View
2. Select "Process Flow" mode
3. Trace upstream/downstream
4. Review connected lines/instruments
```

---

## ⚡ Keyboard Shortcuts & Tips

### Tips for Efficiency

**Tag Renumbering:**
- Use "Select All" for bulk operations
- Test pattern with one equipment first
- Always preview before applying

**Hierarchical View:**
- Double-click to expand/collapse
- Use search box for quick lookup
- Check all tabs for complete information

**Audit Log:**
- Start with "24 Hours" filter for recent changes
- Combine filters for specific searches
- Use "All Time" for forensic analysis

---

## 📊 Quick Stats

| Feature | Capability |
|---------|-----------|
| Sync Speed | ~100 items in < 30 seconds |
| Audit Retention | Unlimited (all history) |
| Renumbering | Unlimited equipment per operation |
| Hierarchy Depth | Unlimited levels |

---

## 🔧 Troubleshooting

### PIDSYNC Issues
**Problem:** Sync fails
**Solution:** Check database connection, verify project selected

**Problem:** Changes not reflected
**Solution:** Ensure ModifiedDate updated, try Full Sync mode

### Audit Log Empty
**Problem:** No entries showing
**Solution:** Check time range filter, verify project selected, click Refresh

### Renumbering Conflicts
**Problem:** Duplicate tag warning
**Solution:** Review preview carefully, adjust pattern or filters

### Hierarchical View Slow
**Problem:** Tree takes time to load
**Solution:** Use filters, start with smaller view (By Type vs Process Flow)

---

## 📞 Need Help?

1. **User Guide:** See [USER_GUIDE_AND_TESTING.md](USER_GUIDE_AND_TESTING.md)
2. **Feature Details:** See [IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md)
3. **Migration:** See [MIGRATION_README.md](MIGRATION_README.md)
4. **Recent Changes:** Check git commit history

---

## ✅ Pre-Flight Checklist

Before using new features:
- [ ] Database migration applied
- [ ] Latest version deployed
- [ ] Test project created
- [ ] Backup taken

**Remember:** All operations create audit log entries for tracking!

---

**Version:** 1.0.4
**Last Updated:** 2026-01-18
**Print this page for quick desk reference**
