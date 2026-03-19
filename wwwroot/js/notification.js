/**
 * notification.js — ComicVerse
 * Quản lý UI notification panel + SignalR real-time.
 * Yêu cầu: signalr.min.js và notyf.min.js phải load TRƯỚC file này.
 */
(function () {
    'use strict';

    const ICON_MAP = {
        comic_approved: 'ph ph-check-circle',
        comic_rejected: 'ph ph-x-circle',
        comic_hidden: 'ph ph-eye-slash',
        new_chapter: 'ph ph-book-open',
        new_pending: 'ph ph-hourglass',
        new_report: 'ph ph-flag',
        account_warning: 'ph ph-warning',
        account_banned: 'ph ph-lock',
        account_unbanned: 'ph ph-lock-open',
        system: 'ph ph-info',
    };

    let notifData = [];

    const $ = (sel, ctx) => (ctx || document).querySelector(sel);
    const $$ = (sel, ctx) => (ctx || document).querySelectorAll(sel);

    document.addEventListener('DOMContentLoaded', function () {
        setupBell();
        setupSignalR();
        loadBadgeCount();
    });

    function setupBell() {
        const btn = $('.notif-bell-btn');
        const dropdown = $('.notif-dropdown');
        if (!btn || !dropdown) return;

        btn.addEventListener('click', function (e) {
            e.stopPropagation();
            const isOpen = dropdown.classList.toggle('open');
            if (isOpen) {
                closeDetail();
                loadNotifications();
            }
        });

        const markAllBtn = $('.notif-mark-all');
        if (markAllBtn) markAllBtn.addEventListener('click', markAllRead);

        document.addEventListener('click', function (e) {
            if (!dropdown.contains(e.target) && !btn.contains(e.target)) {
                dropdown.classList.remove('open');
                closeDetail();
            }
        });
    }

    // Load chỉ số đếm — dùng khi page mới mở
    function loadBadgeCount() {
        fetch('/Notification/List')
            .then(r => r.json())
            .then(data => {
                notifData = data.notifications || [];
                updateBadge(data.unreadCount || 0);
            })
            .catch(() => { });
    }

    // Load đầy đủ — dùng khi mở panel
    function loadNotifications() {
        const list = $('.notif-list');
        if (!list) return;

        list.innerHTML = skeletonHTML();

        fetch('/Notification/List')
            .then(r => r.json())
            .then(data => {
                notifData = data.notifications || [];
                renderList(notifData);
                updateBadge(data.unreadCount || 0);
            })
            .catch(() => {
                list.innerHTML = '<div class="notif-empty"><i class="ph ph-wifi-slash"></i><span>Không thể tải thông báo</span></div>';
            });
    }

    function renderList(notifications) {
        const list = $('.notif-list');
        if (!list) return;

        if (!notifications.length) {
            list.innerHTML = '<div class="notif-empty"><i class="ph ph-bell-slash"></i><span>Bạn chưa có thông báo nào</span></div>';
            return;
        }

        list.innerHTML = notifications.map(n => itemHTML(n)).join('');

        $$('.notif-item', list).forEach(el => {
            el.addEventListener('click', function () {
                openDetail(parseInt(this.dataset.id));
            });
        });
    }

    function itemHTML(n) {
        const icon = ICON_MAP[n.notificationType] || ICON_MAP.system;
        const readClass = n.isRead ? 'read' : 'unread';
        const preview = (n.content || '').replace(/<[^>]+>/g, '').substring(0, 60);

        return `
        <div class="notif-item ${readClass}" data-id="${n.notificationId}">
            <div class="notif-icon type-${n.notificationType}">
                <i class="${icon}"></i>
            </div>
            <div class="notif-body">
                <div class="notif-item-title">${escHtml(n.title)}</div>
                <div class="notif-item-preview">${escHtml(preview)}${preview.length >= 60 ? '…' : ''}</div>
                <div class="notif-item-time">
                    ${!n.isRead ? '<span class="notif-dot"></span>' : ''}
                    <i class="ph ph-clock" style="font-size:0.75rem"></i>
                    ${n.createdAt}
                </div>
            </div>
        </div>`;
    }

    function openDetail(id) {
        const n = notifData.find(x => x.notificationId === id);
        if (!n) return;

        const detail = $('.notif-detail');
        if (!detail) return;

        const icon = ICON_MAP[n.notificationType] || ICON_MAP.system;

        detail.innerHTML = `
            <div class="notif-detail-header">
                <button class="notif-detail-back" onclick="window._notifBack()">
                    <i class="ph ph-arrow-left"></i> Quay lại
                </button>
                <button class="notif-detail-del" title="Xoá" onclick="window._notifDelete(${id})">
                    <i class="ph ph-trash"></i>
                </button>
            </div>
            <div class="notif-detail-title">${escHtml(n.title)}</div>
            <div class="notif-detail-meta">
                <i class="${icon}" style="font-size:0.85rem"></i>
                <span>${n.createdAt}</span>
            </div>
            <div class="notif-detail-body">${(n.content || '').replace(/\n/g, '<br>')}</div>
            <div class="notif-detail-actions">
                ${n.actionUrl
                ? `<a href="${n.actionUrl}" class="notif-btn-action primary"><i class="ph ph-arrow-square-out"></i> Xem chi tiết</a>`
                : ''}
                <button class="notif-btn-action ghost" onclick="window._notifBack()">
                    <i class="ph ph-x"></i> Đóng
                </button>
            </div>`;

        detail.classList.add('open');

        const listEl = $('.notif-list');
        const headerEl = $('.notif-header');
        if (listEl) listEl.style.display = 'none';
        if (headerEl) headerEl.style.display = 'none';

        if (!n.isRead) markRead(id);
    }

    function closeDetail() {
        const detail = $('.notif-detail');
        const listEl = $('.notif-list');
        const headerEl = $('.notif-header');
        if (detail) detail.classList.remove('open');
        if (listEl) listEl.style.display = '';
        if (headerEl) headerEl.style.display = '';
    }

    window._notifBack = closeDetail;

    window._notifDelete = function (id) {
        fetch(`/Notification/Delete/${id}`, {
            method: 'POST',
            headers: { 'RequestVerificationToken': getAntiForgery() }
        })
            .then(r => r.json())
            .then(data => {
                notifData = notifData.filter(n => n.notificationId !== id);
                closeDetail();
                renderList(notifData);
                updateBadge(data.unreadCount);
            });
    };

    function markRead(id) {
        fetch(`/Notification/MarkRead/${id}`, {
            method: 'POST',
            headers: { 'RequestVerificationToken': getAntiForgery() }
        })
            .then(r => r.json())
            .then(data => {
                const n = notifData.find(x => x.notificationId === id);
                if (n) n.isRead = true;
                updateBadge(data.unreadCount);

                const el = $(`.notif-item[data-id="${id}"]`);
                if (el) {
                    el.classList.remove('unread');
                    el.classList.add('read');
                    const dot = el.querySelector('.notif-dot');
                    if (dot) dot.remove();
                }
            });
    }

    function markAllRead() {
        fetch('/Notification/MarkAllRead', {
            method: 'POST',
            headers: { 'RequestVerificationToken': getAntiForgery() }
        })
            .then(r => r.json())
            .then(() => {
                notifData.forEach(n => n.isRead = true);
                renderList(notifData);
                updateBadge(0);
            });
    }

    function updateBadge(count) {
        const badge = $('.notif-badge');
        if (!badge) return;
        if (count > 0) {
            badge.textContent = count > 99 ? '99+' : count;
            badge.classList.add('visible');
        } else {
            badge.classList.remove('visible');
        }
    }

    function setupSignalR() {
        if (typeof signalR === 'undefined') return;

        const connection = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/notification')
            .withAutomaticReconnect()
            .build();

        connection.on('ReceiveNotification', function (notif) {
            notifData.unshift(notif);

            const badge = $('.notif-badge');
            const current = badge ? (parseInt(badge.textContent) || 0) : 0;
            updateBadge(current + 1);

            const dropdown = $('.notif-dropdown');
            if (dropdown && dropdown.classList.contains('open') && !$('.notif-detail.open')) {
                renderList(notifData);
            }

            if (typeof notyf !== 'undefined') {
                notyf.open({
                    type: 'info',
                    message: `<i class="ph ph-bell" style="margin-right:6px"></i>${notif.title}`
                });
            }
        });

        connection.start().catch(err => console.warn('NotificationHub error:', err));
    }

    function escHtml(str) {
        return (str || '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    }

    function getAntiForgery() {
        const el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : '';
    }

    function skeletonHTML() {
        return [1, 2, 3].map(() => `
            <div class="notif-skeleton">
                <div class="sk-circle"></div>
                <div class="sk-lines">
                    <div class="sk-line"></div>
                    <div class="sk-line short"></div>
                    <div class="sk-line shorter"></div>
                </div>
            </div>`).join('');
    }

})();