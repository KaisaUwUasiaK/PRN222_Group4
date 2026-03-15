/* ── Bell button wrapper ────────────────────────────────── */
.notif - bell - wrapper {
    position: relative;
    display: inline - flex;
    align - items: center;
}

.notif - badge {
    position: absolute;
    top: 2px;
    right: 2px;
    background: var(--color - danger);
    color: #fff;
    font - size: 0.6rem;
    font - weight: 700;
    border - radius: var(--radius - full);
    padding: 1px 4px;
    min - width: 16px;
    text - align: center;
    line - height: 1.4;
    pointer - events: none;
    display: none; /* ẩn khi = 0 */
}

.notif - badge.visible {
    display: block;
}

/* ── Dropdown container ─────────────────────────────────── */
.notif - dropdown {
    position: absolute;
    top: calc(100 % + 10px);
    right: 0;
    width: 380px;
    background: var(--bg - surface);
    border: 1px solid var(--border - color);
    border - radius: var(--radius - md);
    box - shadow: 0 20px 40px rgba(0, 0, 0, 0.5);
    z - index: 9999;
    display: none;
    flex - direction: column;
    max - height: 520px;
    overflow: hidden;
}

.notif - dropdown.open {
    display: flex;
}

/* ── Header ─────────────────────────────────────────────── */
.notif - header {
    display: flex;
    align - items: center;
    justify - content: space - between;
    padding: 14px 16px 10px;
    border - bottom: 1px solid var(--border - color);
    flex - shrink: 0;
}

.notif - header - title {
    font - size: 0.95rem;
    font - weight: 600;
    color: var(--text - primary);
    display: flex;
    align - items: center;
    gap: 8px;
}

.notif - header - title i {
    color: var(--color - primary);
    font - size: 1.1rem;
}

.notif - mark - all {
    font - size: 0.75rem;
    color: var(--color - primary);
    background: none;
    border: none;
    cursor: pointer;
    padding: 0;
    font - weight: 500;
    transition: opacity 0.2s;
}

.notif - mark - all:hover {
    opacity: 0.7;
    text - decoration: underline;
}

/* ── List ───────────────────────────────────────────────── */
.notif - list {
    overflow - y: auto;
    flex: 1;
    /* Scrollbar style */
    scrollbar - width: thin;
    scrollbar - color: var(--border - color) transparent;
}

.notif - list:: -webkit - scrollbar { width: 4px; }
.notif - list:: -webkit - scrollbar - track { background: transparent; }
.notif - list:: -webkit - scrollbar - thumb { background: var(--border - color); border - radius: 4px; }

/* ── Item ───────────────────────────────────────────────── */
.notif - item {
    display: flex;
    gap: 10px;
    padding: 12px 16px;
    border - bottom: 1px solid rgba(51, 65, 85, 0.5);
    cursor: pointer;
    transition: background 0.15s;
    position: relative;
}

.notif - item:hover {
    background: var(--bg - surface - hover);
}

.notif - item: last - child {
    border - bottom: none;
}

/* Chỉ thị chưa đọc — viền trái violet */
.notif - item.unread {
    background: rgba(124, 58, 237, 0.06);
}

.notif - item.unread::before {
    content: '';
    position: absolute;
    left: 0;
    top: 0;
    bottom: 0;
    width: 3px;
    background: var(--color - primary);
    border - radius: 0 2px 2px 0;
}

/* ── Icon bubble ────────────────────────────────────────── */
.notif - icon {
    width: 36px;
    height: 36px;
    border - radius: 50 %;
    display: flex;
    align - items: center;
    justify - content: center;
    flex - shrink: 0;
    font - size: 1rem;
    margin - top: 1px;
}

.notif - icon.type - comic_approved  { background: rgba(16, 185, 129, 0.15); color: #10B981; }
.notif - icon.type - comic_rejected  { background: rgba(244, 63, 94, 0.15); color: #F43F5E; }
.notif - icon.type - comic_hidden    { background: rgba(245, 158, 11, 0.15); color: #F59E0B; }
.notif - icon.type - new_chapter     { background: rgba(99, 102, 241, 0.15); color: #6366F1; }
.notif - icon.type - new_pending     { background: rgba(245, 158, 11, 0.15); color: #F59E0B; }
.notif - icon.type - new_report      { background: rgba(244, 63, 94, 0.15); color: #F43F5E; }
.notif - icon.type - account_warning { background: rgba(245, 158, 11, 0.15); color: #F59E0B; }
.notif - icon.type - account_banned  { background: rgba(244, 63, 94, 0.15); color: #F43F5E; }
.notif - icon.type - account_unbanned{ background: rgba(16, 185, 129, 0.15); color: #10B981; }
.notif - icon.type - system          { background: rgba(148, 163, 184, 0.15); color: #94A3B8; }

/* ── Item body ──────────────────────────────────────────── */
.notif - body {
    flex: 1;
    min - width: 0;
}

.notif - item - title {
    font - size: 0.85rem;
    font - weight: 600;
    color: var(--text - primary);
    white - space: nowrap;
    overflow: hidden;
    text - overflow: ellipsis;
    margin - bottom: 3px;
}

.notif - item.read.notif - item - title {
    font - weight: 400;
    color: var(--text - secondary);
}

.notif - item - preview {
    font - size: 0.78rem;
    color: var(--text - muted);
    white - space: nowrap;
    overflow: hidden;
    text - overflow: ellipsis;
}

.notif - item - time {
    font - size: 0.72rem;
    color: var(--text - muted);
    white - space: nowrap;
    margin - top: 4px;
    display: flex;
    align - items: center;
    gap: 4px;
}

/* Chấm xanh unread indicator */
.notif - dot {
    width: 7px;
    height: 7px;
    border - radius: 50 %;
    background: var(--color - primary);
    flex - shrink: 0;
    align - self: center;
}

/* ── Detail panel ───────────────────────────────────────── */
.notif - detail {
    display: none;
    flex - direction: column;
    border - top: 1px solid var(--border - color);
    background: var(--bg - surface);
    flex - shrink: 0;
    max - height: 260px;
}

.notif - detail.open {
    display: flex;
}

.notif - detail - header {
    display: flex;
    align - items: center;
    justify - content: space - between;
    padding: 10px 16px 6px;
    gap: 8px;
}

.notif - detail - back {
    background: none;
    border: none;
    cursor: pointer;
    color: var(--text - muted);
    padding: 4px;
    border - radius: var(--radius - sm);
    display: flex;
    align - items: center;
    gap: 6px;
    font - size: 0.8rem;
    transition: color 0.2s;
}

.notif - detail - back:hover { color: var(--text - primary); }

.notif - detail - del {
    background: none;
    border: none;
    cursor: pointer;
    color: var(--text - muted);
    padding: 4px;
    border - radius: var(--radius - sm);
    font - size: 1rem;
    display: flex;
    align - items: center;
    transition: color 0.2s;
}

.notif - detail - del:hover { color: var(--color - danger); }

.notif - detail - title {
    font - size: 0.9rem;
    font - weight: 600;
    color: var(--text - primary);
    padding: 0 16px 6px;
    line - height: 1.4;
}

.notif - detail - meta {
    font - size: 0.75rem;
    color: var(--text - muted);
    padding: 0 16px 8px;
    display: flex;
    align - items: center;
    gap: 6px;
}

.notif - detail - body {
    font - size: 0.82rem;
    color: var(--text - secondary);
    line - height: 1.65;
    padding: 0 16px;
    overflow - y: auto;
    flex: 1;
    scrollbar - width: thin;
    scrollbar - color: var(--border - color) transparent;
}

.notif - detail - body b { color: var(--text - primary); }

.notif - detail - actions {
    padding: 10px 16px 14px;
    display: flex;
    gap: 8px;
}

.notif - btn - action {
    font - size: 0.78rem;
    padding: 6px 14px;
    border - radius: var(--radius - sm);
    border: none;
    cursor: pointer;
    font - weight: 500;
    text - decoration: none;
    display: inline - flex;
    align - items: center;
    gap: 6px;
    transition: background 0.15s, color 0.15s;
}

.notif - btn - action.primary {
    background: var(--color - primary);
    color: #fff;
}

.notif - btn - action.primary:hover {
    background: var(--color - primary - hover);
}

.notif - btn - action.ghost {
    background: var(--bg - surface - hover);
    color: var(--text - secondary);
}

.notif - btn - action.ghost:hover {
    color: var(--text - primary);
}

/* ── Empty state ────────────────────────────────────────── */
.notif - empty {
    display: flex;
    flex - direction: column;
    align - items: center;
    justify - content: center;
    gap: 10px;
    padding: 40px 20px;
    color: var(--text - muted);
}

.notif - empty i {
    font - size: 2.5rem;
    opacity: 0.4;
}

.notif - empty span {
    font - size: 0.85rem;
}

/* ── Loading skeleton ───────────────────────────────────── */
.notif - skeleton {
    padding: 12px 16px;
    display: flex;
    gap: 10px;
    border - bottom: 1px solid rgba(51, 65, 85, 0.5);
}

.sk - circle {
    width: 36px;
    height: 36px;
    border - radius: 50 %;
    background: var(--bg - surface - hover);
    flex - shrink: 0;
    animation: sk - pulse 1.5s ease -in -out infinite;
}

.sk - lines {
    flex: 1;
    display: flex;
    flex - direction: column;
    gap: 6px;
    justify - content: center;
}

.sk - line {
    height: 10px;
    border - radius: 4px;
    background: var(--bg - surface - hover);
    animation: sk - pulse 1.5s ease -in -out infinite;
}

.sk - line.short { width: 55 %; }
.sk - line.shorter { width: 35 %; }

@keyframes sk - pulse {
    0 %, 100 % { opacity: 0.6; }
    50 % { opacity: 0.3; }
}