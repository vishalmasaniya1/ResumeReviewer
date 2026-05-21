/**
 * TalentAI SPA Router and Controller Client
 */

const API_BASE = window.location.origin + '/api';

// State Management
const State = {
    token: localStorage.getItem('talent_token'),
    user: JSON.parse(localStorage.getItem('talent_user') || 'null'),
    currentView: 'dashboard',
    activePollingJobs: {}, // tracks taskId -> intervalId for matching
    cachedJobs: [],
    recentMatches: [],
    systemStats: null
};

// Global Initialization
document.addEventListener('DOMContentLoaded', () => {
    initApp();
});

function initApp() {
    setupAuthHeaders();
    setupEventListeners();
    handleRouting();
    
    // Register routing event
    window.addEventListener('hashchange', handleRouting);
    
    // Check if logged in
    if (!State.token) {
        showView('login');
    } else {
        updateSidebarProfile();
        // Load background configurations
        fetchSystemStatus();
    }
}

// ---------------------------------------------------------
// AUTHENTICATION & HEADERS
// ---------------------------------------------------------
function setupAuthHeaders() {
    if (State.token) {
        // We will pass Bearer tokens to all API fetch calls
    }
}

function updateSidebarProfile() {
    const userDisplay = document.getElementById('user-display-name');
    if (userDisplay && State.user) {
        userDisplay.textContent = State.user.username || 'Recruiter';
    }
}

async function apiFetch(endpoint, options = {}) {
    const headers = options.headers || {};
    if (State.token) {
        headers['Authorization'] = `Bearer ${State.token}`;
    }
    
    if (!(options.body instanceof FormData) && !headers['Content-Type']) {
        headers['Content-Type'] = 'application/json';
    }
    
    options.headers = headers;
    
    try {
        const response = await fetch(`${API_BASE}/${endpoint}`, options);
        
        if (response.status === 401) {
            logout();
            throw new Error("Session expired. Please log in again.");
        }
        
        if (response.status === 204) {
            return null;
        }
        
        const data = await response.json();
        
        if (!response.ok) {
            const errorMsg = data?.detail || data?.title || `API error: ${response.statusText}`;
            throw new Error(errorMsg);
        }
        
        return data;
    } catch (err) {
        console.error(`API Fetch Error [${endpoint}]:`, err);
        throw err;
    }
}

function logout() {
    localStorage.removeItem('talent_token');
    localStorage.removeItem('talent_user');
    State.token = null;
    State.user = null;
    
    // Clear all polling
    Object.values(State.activePollingJobs).forEach(clearInterval);
    State.activePollingJobs = {};
    
    showView('login');
    showToast('Info', 'You have been logged out.', 'info');
}

// ---------------------------------------------------------
// EVENT LISTENERS Setup
// ---------------------------------------------------------
function setupEventListeners() {
    // Login Submit
    const loginForm = document.getElementById('login-form');
    if (loginForm) {
        loginForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const email = document.getElementById('login-email').value;
            const password = document.getElementById('login-password').value;
            const loginError = document.getElementById('login-error');
            
            loginError.classList.add('hidden');
            
            try {
                const response = await apiFetch('auth/login', {
                    method: 'POST',
                    body: JSON.stringify({ email, password })
                });
                
                if (response && response.token) {
                    State.token = response.token;
                    State.user = { username: response.username, email: email };
                    localStorage.setItem('talent_token', response.token);
                    localStorage.setItem('talent_user', JSON.stringify(State.user));
                    
                    updateSidebarProfile();
                    showToast('Welcome!', 'Logged in successfully.', 'success');
                    
                    window.location.hash = '#dashboard';
                }
            } catch (err) {
                loginError.classList.remove('hidden');
                loginError.querySelector('span').textContent = err.message || 'Login failed.';
            }
        });
    }
    
    // Sidebar Links active highlight
    const menuItems = document.querySelectorAll('.sidebar-menu .menu-item');
    menuItems.forEach(item => {
        item.addEventListener('click', (e) => {
            // Only update active on non-external links
            if (item.getAttribute('href').startsWith('#')) {
                menuItems.forEach(m => m.classList.remove('active'));
                item.classList.add('active');
            }
        });
    });
    
    // Logout Button
    const logoutBtn = document.getElementById('logout-btn');
    if (logoutBtn) {
        logoutBtn.addEventListener('click', logout);
    }
    
    // Job Modal Management
    const openJobBtn = document.getElementById('open-job-modal-btn');
    const jobModal = document.getElementById('job-modal');
    const closeModals = document.querySelectorAll('.btn-close-modal');
    
    if (openJobBtn) {
        openJobBtn.addEventListener('click', () => {
            jobModal.classList.remove('hidden');
        });
    }
    
    closeModals.forEach(btn => {
        btn.addEventListener('click', () => {
            jobModal.classList.add('hidden');
        });
    });
    
    // Create Job Submission
    const jobForm = document.getElementById('job-form');
    if (jobForm) {
        jobForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const payload = {
                title: document.getElementById('job-title').value,
                department: document.getElementById('job-department').value,
                location: document.getElementById('job-location').value,
                experienceYearsMin: parseInt(document.getElementById('job-experience').value) || 0,
                content: document.getElementById('job-content').value,
                requiredSkills: document.getElementById('job-skills').value.split(',').map(s => s.trim()).filter(Boolean)
            };
            
            try {
                await apiFetch('jobs', {
                    method: 'POST',
                    body: JSON.stringify(payload)
                });
                
                showToast('Success', 'Job opening added successfully!', 'success');
                jobModal.classList.add('hidden');
                jobForm.reset();
                
                // Refresh list if on jobs view
                if (State.currentView === 'jobs') {
                    loadJobsView();
                }
            } catch (err) {
                showToast('Error', err.message, 'error');
            }
        });
    }
    
    // Drag and Drop Upload Dropzone
    const dropzone = document.getElementById('resume-dropzone');
    const fileInput = document.getElementById('resume-file-input');
    const removeFileBtn = document.querySelector('.btn-remove-file');
    
    if (dropzone && fileInput) {
        dropzone.addEventListener('click', (e) => {
            if (e.target.closest('.btn-remove-file')) return; // ignore remove click
            fileInput.click();
        });
        
        fileInput.addEventListener('change', () => {
            handleSelectedFile(fileInput.files[0]);
        });
        
        dropzone.addEventListener('dragover', (e) => {
            e.preventDefault();
            dropzone.classList.add('drag-active');
        });
        
        dropzone.addEventListener('dragleave', () => {
            dropzone.classList.remove('drag-active');
        });
        
        dropzone.addEventListener('drop', (e) => {
            e.preventDefault();
            dropzone.classList.remove('drag-active');
            if (e.dataTransfer.files.length > 0) {
                fileInput.files = e.dataTransfer.files;
                handleSelectedFile(fileInput.files[0]);
            }
        });
    }
    
    if (removeFileBtn) {
        removeFileBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            resetDropzone();
        });
    }
    
    // Screening Form Submit
    const screeningForm = document.getElementById('screening-form');
    if (screeningForm) {
        screeningForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const jobId = document.getElementById('screen-job-id').value;
            const file = fileInput.files[0];
            
            if (!jobId || !file) {
                showToast('Warning', 'Please select a job description and upload a resume.', 'error');
                return;
            }
            
            const formData = new FormData();
            formData.append('file', file);
            formData.append('email', document.getElementById('screen-candidate-email').value);
            formData.append('firstName', document.getElementById('screen-candidate-first-name').value);
            formData.append('lastName', document.getElementById('screen-candidate-last-name').value);
            formData.append('phone', document.getElementById('screen-candidate-phone').value);
            
            const btn = document.getElementById('btn-start-screening');
            const originalHtml = btn.innerHTML;
            btn.disabled = true;
            btn.innerHTML = `<i class="fa-solid fa-spinner fa-spin"></i> Processing...`;
            
            try {
                // Upload Resume -> initiates parsing and schedules matching
                const resumeResponse = await apiFetch('resumes', {
                    method: 'POST',
                    body: formData
                });
                
                showToast('Processing', 'Resume uploaded. Matching enqueued.', 'success');
                
                // Add task to our visual screening queue
                addResumeToScreeningQueue(resumeResponse.id, resumeResponse.fileName, jobId);
                
                // Reset Form
                screeningForm.reset();
                resetDropzone();
            } catch (err) {
                showToast('Upload Failed', err.message, 'error');
            } finally {
                btn.disabled = false;
                btn.innerHTML = originalHtml;
            }
        });
    }
    
    // Detail View Back Button
    const backBtn = document.getElementById('back-to-dashboard-btn');
    if (backBtn) {
        backBtn.addEventListener('click', () => {
            window.location.hash = '#dashboard';
        });
    }

    // Detail View Tabs
    const tabBtns = document.querySelectorAll('.panel-tabs .tab-btn');
    tabBtns.forEach(btn => {
        btn.addEventListener('click', () => {
            tabBtns.forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            
            const targetTab = btn.getAttribute('data-tab');
            document.querySelectorAll('.tab-content').forEach(c => c.classList.add('hidden'));
            document.getElementById(`tab-${targetTab}`).classList.remove('hidden');
        });
    });

    // Dashboard Match Filter
    const jobFilter = document.getElementById('dashboard-job-filter');
    if (jobFilter) {
        jobFilter.addEventListener('change', () => {
            renderMatchesTable(jobFilter.value);
        });
    }
}

// Helper: Handle file selected in dropzone
function handleSelectedFile(file) {
    if (!file) return;
    
    const dropzoneContent = document.querySelector('#resume-dropzone .dropzone-content');
    const selectedArea = document.querySelector('#resume-dropzone .dropzone-file-selected');
    const fileNameEl = selectedArea.querySelector('.file-name');
    const fileSizeEl = selectedArea.querySelector('.file-size');
    const iconEl = selectedArea.querySelector('.file-type-icon');
    
    // Format size
    const sizeStr = file.size > 1024 * 1024 
        ? `${(file.size / (1024*1024)).toFixed(1)} MB` 
        : `${(file.size / 1024).toFixed(0)} KB`;
        
    fileNameEl.textContent = file.name;
    fileSizeEl.textContent = sizeStr;
    
    // Set icon
    if (file.name.endsWith('.docx')) {
        iconEl.className = 'fa-solid fa-file-word file-type-icon';
        iconEl.style.color = '#2b579a';
    } else {
        iconEl.className = 'fa-solid fa-file-pdf file-type-icon';
        iconEl.style.color = '#f43f5e';
    }
    
    dropzoneContent.classList.add('hidden');
    selectedArea.classList.remove('hidden');
}

function resetDropzone() {
    const fileInput = document.getElementById('resume-file-input');
    if (fileInput) fileInput.value = '';
    
    const dropzoneContent = document.querySelector('#resume-dropzone .dropzone-content');
    const selectedArea = document.querySelector('#resume-dropzone .dropzone-file-selected');
    
    if (dropzoneContent && selectedArea) {
        dropzoneContent.classList.remove('hidden');
        selectedArea.classList.add('hidden');
    }
}

// ---------------------------------------------------------
// ROUTER & VIEW CONTROLLER
// ---------------------------------------------------------
function handleRouting() {
    const hash = window.location.hash || '#dashboard';
    
    // Ensure auth
    if (!State.token && hash !== '#login') {
        window.location.hash = '#login';
        return;
    }
    
    if (hash === '#login' || hash === '') {
        showView('login');
        return;
    }
    
    showView('app');
    
    // Sub-view routes parsing
    const cleanHash = hash.substring(1);
    
    // Check if match detail view: e.g. match-details?id=UUID
    if (cleanHash.startsWith('match-details')) {
        const urlParams = new URLSearchParams(cleanHash.substring(cleanHash.indexOf('?')));
        const matchId = urlParams.get('id');
        if (matchId) {
            State.currentView = 'match-details';
            toggleSubView('match-details');
            loadMatchDetailsView(matchId);
        } else {
            window.location.hash = '#dashboard';
        }
        return;
    }
    
    State.currentView = cleanHash;
    toggleSubView(cleanHash);
    
    // Trigger specific view loaders
    switch (cleanHash) {
        case 'dashboard':
            loadDashboardView();
            break;
        case 'jobs':
            loadJobsView();
            break;
        case 'screening':
            loadScreeningView();
            break;
        case 'analytics':
            loadAnalyticsView();
            break;
    }
}

function showView(view) {
    const loginView = document.getElementById('login-view');
    const appShell = document.getElementById('app-shell');
    
    if (view === 'login') {
        loginView.classList.remove('hidden');
        appShell.classList.add('hidden');
    } else {
        loginView.classList.add('hidden');
        appShell.classList.remove('hidden');
    }
}

function toggleSubView(viewId) {
    // Hide all view sections
    document.querySelectorAll('.app-view').forEach(view => {
        view.classList.add('hidden');
    });
    
    // Show target section
    const target = document.getElementById(`view-${viewId}`);
    if (target) {
        target.classList.remove('hidden');
    }
    
    // Update active state in sidebar menu
    const menuItems = document.querySelectorAll('.sidebar-menu .menu-item');
    menuItems.forEach(item => {
        const itemHash = item.getAttribute('href');
        if (itemHash === `#${viewId}`) {
            item.classList.add('active');
        } else {
            item.classList.remove('active');
        }
    });
}

// ---------------------------------------------------------
// DATA FETCHERS & RENDERING
// ---------------------------------------------------------

async function fetchSystemStatus() {
    try {
        const response = await fetch(`${API_BASE}/jobs`, {
            headers: { 'Authorization': `Bearer ${State.token}` }
        });
        // We look at options configured in the system. The server doesn't output the AI options explicitly,
        // so we infer based on provider from appsettings.json or hardcode mock. If local endpoint fails,
        // we fallback to mock display. 
        document.getElementById('ai-provider-badge').textContent = 'SQLite + AI Simulator';
    } catch {
        document.getElementById('ai-provider-badge').textContent = 'Mock';
    }
}

// --- 1. DASHBOARD VIEW ---
async function loadDashboardView() {
    try {
        // Fetch dashboard stats summary
        const summary = await apiFetch('analytics');
        State.systemStats = summary;
        
        // Populate stats cards
        document.getElementById('stat-total-resumes').textContent = summary.totalResumes || '0';
        document.getElementById('stat-active-jobs').textContent = summary.totalJobs || '0';
        document.getElementById('stat-avg-score').textContent = summary.averageMatchScore ? `${summary.averageMatchScore}%` : '0%';
        document.getElementById('stat-pending-reviews').textContent = summary.pendingReviews || '0';
        
        // Populated Recent Matches
        State.recentMatches = summary.recentMatches || [];
        
        // Populate job filter dropdown
        const filter = document.getElementById('dashboard-job-filter');
        const currentSelection = filter.value;
        filter.innerHTML = `<option value="">All Job Descriptions</option>`;
        
        const jobs = await apiFetch('jobs');
        State.cachedJobs = jobs;
        jobs.forEach(job => {
            const opt = document.createElement('option');
            opt.value = job.id;
            opt.textContent = job.title;
            filter.appendChild(opt);
        });
        filter.value = currentSelection;
        
        renderMatchesTable(filter.value);
        renderRecentActivities(summary.recentMatches);
        
    } catch (err) {
        showToast('Error', 'Failed to load dashboard metrics.', 'error');
    }
}

function renderMatchesTable(jobIdFilter = "") {
    const tbody = document.getElementById('top-matches-table-body');
    if (!tbody) return;
    
    tbody.innerHTML = '';
    
    let filtered = State.recentMatches;
    if (jobIdFilter) {
        filtered = State.recentMatches.filter(m => m.jobId === jobIdFilter);
    }
    
    if (filtered.length === 0) {
        tbody.innerHTML = `<tr><td colspan="6" class="text-center">No matching candidate evaluations found.</td></tr>`;
        return;
    }
    
    filtered.forEach(match => {
        const score = Math.round(match.matchScore);
        let badgeClass = 'badge success';
        if (score < 50) badgeClass = 'badge danger';
        else if (score < 70) badgeClass = 'badge warning';
        else if (score < 85) badgeClass = 'badge info';
        
        let statusBadge = 'badge info';
        let statusText = 'Pending';
        if (match.status === 2 || match.status === 'Completed') {
            statusBadge = 'badge success';
            statusText = 'Completed';
        } else if (match.status === 1 || match.status === 'Processing') {
            statusBadge = 'badge warning';
            statusText = 'Processing';
        } else if (match.status === 3 || match.status === 'Failed') {
            statusBadge = 'badge danger';
            statusText = 'Failed';
        }

        const date = new Date(match.processedAt || match.createdAt || Date.now()).toLocaleDateString(undefined, {
            month: 'short',
            day: 'numeric',
            year: 'numeric'
        });
        
        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td><strong>${match.candidateName || 'Candidate'}</strong></td>
            <td>${match.jobTitle || 'Requisition'}</td>
            <td><span class="${badgeClass}">${score}%</span></td>
            <td><span class="${statusBadge}">${statusText}</span></td>
            <td>${date}</td>
            <td>
                <a href="#match-details?id=${match.id}" class="btn btn-secondary btn-sm" style="padding: 4px 10px; font-size: 11px;">
                    <i class="fa-solid fa-folder-open"></i> View Review
                </a>
            </td>
        `;
        tbody.appendChild(tr);
    });
}

function renderRecentActivities(recentMatches) {
    const list = document.getElementById('recent-activities-list');
    if (!list) return;
    
    list.innerHTML = '';
    
    if (!recentMatches || recentMatches.length === 0) {
        list.innerHTML = `<div class="timeline-empty">No recent screening activities</div>`;
        return;
    }
    
    recentMatches.forEach(activity => {
        let dotColor = 'info';
        let actionDesc = 'Screening scheduled';
        
        if (activity.status === 2 || activity.status === 'Completed') {
            dotColor = 'success';
            actionDesc = `Evaluated match at <strong>${Math.round(activity.matchScore)}%</strong> for <strong>${activity.candidateName}</strong>`;
        } else if (activity.status === 3 || activity.status === 'Failed') {
            dotColor = 'danger';
            actionDesc = `Match failed for <strong>${activity.candidateName}</strong>`;
        } else {
            actionDesc = `Processing <strong>${activity.candidateName}</strong> profile`;
        }
        
        const time = new Date(activity.processedAt || activity.createdAt || Date.now()).toLocaleTimeString(undefined, {
            hour: '2-digit',
            minute: '2-digit'
        });
        
        const item = document.createElement('div');
        item.className = 'timeline-item';
        item.innerHTML = `
            <div class="timeline-dot ${dotColor}"></div>
            <div class="timeline-content">
                <span class="timeline-title">${actionDesc}</span>
                <span class="timeline-time">${time} - ${activity.jobTitle}</span>
            </div>
        `;
        list.appendChild(item);
    });
}

// --- 2. JOBS VIEW ---
async function loadJobsView() {
    const container = document.getElementById('jobs-cards-container');
    if (!container) return;
    
    container.innerHTML = `<div style="grid-column: 1/-1; text-align: center; color: var(--text-muted);">Loading open requisitions...</div>`;
    
    try {
        const jobs = await apiFetch('jobs');
        State.cachedJobs = jobs;
        
        container.innerHTML = '';
        
        if (jobs.length === 0) {
            container.innerHTML = `<div style="grid-column: 1/-1; text-align: center; color: var(--text-muted); padding: 40px 0;">No active job requisitions. Click 'Create Job Description' to initialize a baseline profile.</div>`;
            return;
        }
        
        jobs.forEach(job => {
            const card = document.createElement('div');
            card.className = 'job-card';
            card.innerHTML = `
                <div class="job-card-header">
                    <span class="job-card-title">${job.title}</span>
                    <button class="btn-logout delete-job-btn" data-id="${job.id}" title="Archive/Delete Requisition">
                        <i class="fa-regular fa-trash-can"></i>
                    </button>
                </div>
                <div class="job-card-meta">
                    <span><i class="fa-solid fa-building"></i> ${job.department}</span>
                    <span><i class="fa-solid fa-location-dot"></i> ${job.location}</span>
                    <span><i class="fa-solid fa-clock"></i> Min. Experience: ${job.experienceYearsMin} yrs</span>
                </div>
                <p class="job-card-desc">${job.content}</p>
                <div class="job-card-footer">
                    <span class="skills-preview-count">${job.requiredSkills ? job.requiredSkills.length : 0} Required Skills</span>
                    <button class="btn btn-secondary btn-sm" onclick="window.location.hash='#screening'; document.getElementById('screen-job-id').value='${job.id}';">
                        Screen Profile
                    </button>
                </div>
            `;
            
            // Delete Requisition
            card.querySelector('.delete-job-btn').addEventListener('click', async (e) => {
                e.stopPropagation();
                if (confirm(`Are you sure you want to delete the job description for ${job.title}? This will delete all associated screening matches.`)) {
                    try {
                        await apiFetch(`jobs/${job.id}`, { method: 'DELETE' });
                        showToast('Deleted', 'Requisition deleted successfully.', 'success');
                        loadJobsView();
                    } catch (err) {
                        showToast('Error', err.message, 'error');
                    }
                }
            });
            
            container.appendChild(card);
        });
    } catch (err) {
        container.innerHTML = `<div style="grid-column: 1/-1; text-align: center; color: var(--accent-danger);">Failed to load requisitions.</div>`;
    }
}

// --- 3. SCREENING ZONE VIEW ---
async function loadScreeningView() {
    const select = document.getElementById('screen-job-id');
    if (!select) return;
    
    // Save current job selection
    const prevSelection = select.value;
    select.innerHTML = '<option value="" disabled selected>Select a job description...</option>';
    
    try {
        const jobs = await apiFetch('jobs');
        State.cachedJobs = jobs;
        
        jobs.forEach(job => {
            const opt = document.createElement('option');
            opt.value = job.id;
            opt.textContent = `${job.title} (${job.department} - ${job.location})`;
            select.appendChild(opt);
        });
        
        if (prevSelection) {
            select.value = prevSelection;
        }
    } catch (err) {
        showToast('Error', 'Failed to load jobs for screening selection.', 'error');
    }
}

// Screening status tracking and polling
function addResumeToScreeningQueue(resumeId, fileName, jobId) {
    const container = document.getElementById('queue-status-container');
    const emptyEl = container.querySelector('.queue-empty');
    if (emptyEl) emptyEl.remove();
    
    const queueId = `queue-item-${resumeId}`;
    
    // Create card
    const item = document.createElement('div');
    item.className = 'queue-item';
    item.id = queueId;
    
    const job = State.cachedJobs.find(j => j.id === jobId);
    const jobTitle = job ? job.title : 'Requisition';
    
    item.innerHTML = `
        <div class="queue-item-header">
            <span class="queue-item-title">${fileName}</span>
            <span class="badge info" id="${queueId}-status">Enqueued</span>
        </div>
        <div class="queue-item-meta">Matching against: ${jobTitle}</div>
        <div class="queue-progress-bar">
            <div class="queue-progress-fill active" id="${queueId}-progress" style="width: 15%;"></div>
        </div>
    `;
    
    container.insertBefore(item, container.firstChild);
    
    // Start Polling API for completion of matches
    // Since background matching calculates match profiles for all jobs, we search for the specific match record.
    // We poll `GET /api/matches` and filter by resumeId + jobId.
    let ticks = 0;
    const interval = setInterval(async () => {
        ticks++;
        const progressFill = document.getElementById(`${queueId}-progress`);
        const statusBadge = document.getElementById(`${queueId}-status`);
        
        if (ticks > 60) { // Timeout after 5 minutes (5s * 60)
            clearInterval(interval);
            if (statusBadge) {
                statusBadge.textContent = 'Timeout';
                statusBadge.className = 'badge danger';
            }
            if (progressFill) {
                progressFill.className = 'queue-progress-fill failed';
            }
            showToast('Timeout', `Processing of ${fileName} timed out.`, 'error');
            return;
        }
        
        // Update visual tick
        if (progressFill && ticks < 20) {
            progressFill.style.width = `${15 + ticks * 4}%`;
        }
        
        try {
            const matches = await apiFetch('matches');
            const matchRecord = matches.find(m => m.resumeId === resumeId && m.jobId === jobId);
            
            if (matchRecord) {
                const isCompleted = matchRecord.status === 2 || matchRecord.status === 'Completed';
                const isFailed = matchRecord.status === 3 || matchRecord.status === 'Failed';
                const isProcessing = matchRecord.status === 1 || matchRecord.status === 'Processing';
                
                if (statusBadge) {
                    if (isCompleted) {
                        statusBadge.textContent = 'Ready';
                        statusBadge.className = 'badge success';
                    } else if (isProcessing) {
                        statusBadge.textContent = 'Matching';
                        statusBadge.className = 'badge warning';
                    }
                }
                
                if (isCompleted) {
                    clearInterval(interval);
                    delete State.activePollingJobs[resumeId];
                    
                    if (progressFill) {
                        progressFill.className = 'queue-progress-fill';
                        progressFill.style.width = '100%';
                    }
                    
                    showToast('Match Complete', `Screening for ${fileName} is ready! Score: ${Math.round(matchRecord.matchScore)}%`, 'success');
                    
                    // Add details view button to the queue item
                    const detailBtn = document.createElement('a');
                    detailBtn.href = `#match-details?id=${matchRecord.id}`;
                    detailBtn.className = 'btn btn-primary btn-sm';
                    detailBtn.style.padding = '5px 12px';
                    detailBtn.style.fontSize = '12px';
                    detailBtn.style.marginTop = '8px';
                    detailBtn.innerHTML = `<i class="fa-solid fa-chart-line"></i> View Insights`;
                    item.appendChild(detailBtn);
                } 
                else if (isFailed) {
                    clearInterval(interval);
                    delete State.activePollingJobs[resumeId];
                    
                    if (statusBadge) {
                        statusBadge.textContent = 'Failed';
                        statusBadge.className = 'badge danger';
                    }
                    if (progressFill) {
                        progressFill.className = 'queue-progress-fill failed';
                    }
                    showToast('Failed', `AI evaluation failed for ${fileName}`, 'error');
                }
            }
        } catch (err) {
            console.error('Polling error', err);
        }
    }, 5000);
    
    State.activePollingJobs[resumeId] = interval;
}

// --- 4. CANDIDATE MATCH DETAILS VIEW ---
async function loadMatchDetailsView(matchId) {
    // Show spinner/loading defaults
    document.getElementById('match-gauge-value').textContent = '--';
    document.getElementById('detail-candidate-name').textContent = 'Loading...';
    
    try {
        const details = await apiFetch(`matches/${matchId}`);
        
        // Core Candidate Details
        document.getElementById('detail-candidate-name').textContent = details.candidateName || 'Unknown';
        document.getElementById('detail-job-title').textContent = details.jobTitle || 'Unknown Position';
        document.getElementById('detail-match-status').textContent = details.status === 2 || details.status === 'Completed' ? 'Completed' : 'Pending';
        document.getElementById('detail-match-status').className = `badge ${details.status === 2 || details.status === 'Completed' ? 'success' : 'warning'}`;
        
        // Gauge percentage
        Components.renderRadialGauge('match-gauge-value', details.matchScore);
        
        // Fetch original resume info for profile
        const resumeDetails = await apiFetch(`resumes/${details.resumeId}`);
        document.getElementById('detail-candidate-email').textContent = resumeDetails.candidate?.email || '-';
        document.getElementById('detail-candidate-phone').textContent = resumeDetails.candidate?.phone || '-';
        document.getElementById('detail-experience-years').textContent = `${resumeDetails.experienceYears || 0} Years`;
        
        // Set up resume file download button
        const downloadBtn = document.getElementById('btn-download-resume');
        downloadBtn.href = `${API_BASE}/resumes/${details.resumeId}/file`;
        
        // Render Strengths (Bullet points)
        const strengthsList = document.getElementById('detail-strengths-list');
        strengthsList.innerHTML = '';
        if (details.strengths && details.strengths.length > 0) {
            details.strengths.forEach(str => {
                const li = document.createElement('li');
                li.textContent = str;
                strengthsList.appendChild(li);
            });
        } else {
            strengthsList.innerHTML = '<li>No specific strength highlights analyzed.</li>';
        }
        
        // Render Missing Skills (Glowing tags)
        const missingTags = document.getElementById('detail-missing-skills-tags');
        missingTags.innerHTML = '';
        if (details.missingSkills && details.missingSkills.length > 0) {
            details.missingSkills.forEach(skill => {
                const tag = document.createElement('span');
                tag.className = 'tag missing';
                tag.textContent = skill;
                missingTags.appendChild(tag);
            });
        } else {
            missingTags.innerHTML = '<span class="text-secondary" style="font-size:12px;">None! Perfect skill alignment.</span>';
        }
        
        // Render ATS Improvements (Bullet points)
        const atsList = document.getElementById('detail-ats-improvements-list');
        atsList.innerHTML = '';
        if (details.atsImprovements && details.atsImprovements.length > 0) {
            details.atsImprovements.forEach(imp => {
                const li = document.createElement('li');
                li.textContent = imp;
                atsList.appendChild(li);
            });
        } else {
            atsList.innerHTML = '<li>Resume structure matches core parsing profiles.</li>';
        }
        
        // Render Feedback Review Text
        document.getElementById('detail-feedback-text').textContent = details.feedbackText || 'Evaluation pending processing execution.';
        
        // Render Recommended Interview Questions
        const qContainer = document.getElementById('detail-questions-container');
        qContainer.innerHTML = '';
        if (details.interviewQuestions && details.interviewQuestions.length > 0) {
            details.interviewQuestions.forEach(q => {
                const card = document.createElement('div');
                card.className = 'question-card';
                card.innerHTML = `<p class="question-text">"${q}"</p>`;
                qContainer.appendChild(card);
            });
        } else {
            qContainer.innerHTML = '<p class="text-secondary" style="font-size:12px;">No interview questions suggested.</p>';
        }
        
        // Render Raw Parsed Resume Details Tab
        // Render Education
        const eduTimeline = document.getElementById('detail-education-timeline');
        eduTimeline.innerHTML = '';
        try {
            const eduData = typeof resumeDetails.education === 'string' ? JSON.parse(resumeDetails.education) : resumeDetails.education;
            if (Array.isArray(eduData) && eduData.length > 0) {
                eduData.forEach(edu => {
                    const card = document.createElement('div');
                    card.className = 'timeline-card';
                    card.innerHTML = `
                        <div class="timeline-card-header">
                            <span>${edu.degree || 'Degree'} in ${edu.field || 'Field of Study'}</span>
                            <span class="text-muted">${edu.year || ''}</span>
                        </div>
                        <div class="timeline-card-desc">${edu.institution || 'University/Institution'}</div>
                    `;
                    eduTimeline.appendChild(card);
                });
            } else {
                eduTimeline.innerHTML = '<p class="text-secondary" style="font-size:12px;">No education details parsed.</p>';
            }
        } catch {
            eduTimeline.innerHTML = `<p class="timeline-card-desc">${resumeDetails.education || 'No details parsed.'}</p>`;
        }
        
        // Render Projects
        const projTimeline = document.getElementById('detail-projects-timeline');
        projTimeline.innerHTML = '';
        try {
            const projData = typeof resumeDetails.projects === 'string' ? JSON.parse(resumeDetails.projects) : resumeDetails.projects;
            if (Array.isArray(projData) && projData.length > 0) {
                projData.forEach(proj => {
                    const techTags = proj.technologies ? proj.technologies.map(t => `<span class="tag" style="margin-top:5px; padding: 2px 6px; font-size:10px;">${t}</span>`).join(' ') : '';
                    const card = document.createElement('div');
                    card.className = 'timeline-card';
                    card.innerHTML = `
                        <div class="timeline-card-header">
                            <span>${proj.title || 'Project Title'}</span>
                        </div>
                        <div class="timeline-card-desc" style="margin-bottom:8px;">${proj.description || ''}</div>
                        <div class="tags-container">${techTags}</div>
                    `;
                    projTimeline.appendChild(card);
                });
            } else {
                projTimeline.innerHTML = '<p class="text-secondary" style="font-size:12px;">No projects details parsed.</p>';
            }
        } catch {
            projTimeline.innerHTML = `<p class="timeline-card-desc">${resumeDetails.projects || 'No details parsed.'}</p>`;
        }
        
        // Render all Skills Tags
        const allSkillsTags = document.getElementById('detail-all-skills-tags');
        allSkillsTags.innerHTML = '';
        if (resumeDetails.skills && resumeDetails.skills.length > 0) {
            resumeDetails.skills.forEach(skill => {
                const tag = document.createElement('span');
                tag.className = 'tag';
                tag.textContent = skill;
                allSkillsTags.appendChild(tag);
            });
        } else {
            allSkillsTags.innerHTML = '<span class="text-secondary" style="font-size:12px;">No skills extracted.</span>';
        }
        
        // Render Certifications Tags
        const certTags = document.getElementById('detail-certifications-tags');
        certTags.innerHTML = '';
        if (resumeDetails.certifications && resumeDetails.certifications.length > 0) {
            resumeDetails.certifications.forEach(cert => {
                const tag = document.createElement('span');
                tag.className = 'tag';
                tag.textContent = cert;
                certTags.appendChild(tag);
            });
        } else {
            certTags.innerHTML = '<span class="text-secondary" style="font-size:12px;">No certifications parsed.</span>';
        }
        
        // Re-run Screening Button
        const reScreenBtn = document.getElementById('btn-re-screen');
        reScreenBtn.onclick = async () => {
            const originalHtml = reScreenBtn.innerHTML;
            reScreenBtn.disabled = true;
            reScreenBtn.innerHTML = `<i class="fa-solid fa-spinner fa-spin"></i> Queuing...`;
            
            try {
                await apiFetch(`matches/${matchId}/rerun`, { method: 'POST' });
                showToast('Enqueued', 'Screening matching job enqueued for re-run.', 'success');
                
                // Switch back to dashboard and monitor queue status
                addResumeToScreeningQueue(details.resumeId, resumeDetails.fileName, details.jobId);
                window.location.hash = '#screening';
            } catch (err) {
                showToast('Error', err.message, 'error');
            } finally {
                reScreenBtn.disabled = false;
                reScreenBtn.innerHTML = originalHtml;
            }
        };

    } catch (err) {
        showToast('Error', 'Failed to retrieve match evaluation details.', 'error');
        window.location.hash = '#dashboard';
    }
}

// --- 5. ANALYTICS VIEW ---
async function loadAnalyticsView() {
    const summaryContainer = document.getElementById('analytics-summary-metrics');
    if (!summaryContainer) return;
    
    summaryContainer.innerHTML = '<div class="text-center" style="grid-column:1/-1;">Loading system metrics...</div>';
    
    try {
        const stats = await apiFetch('analytics');
        const matches = await apiFetch('matches');
        
        // Compute distribution buckets
        const completed = matches.filter(m => m.status === 2 || m.status === 'Completed');
        const buckets = [0, 0, 0, 0]; // 0-50, 50-70, 70-85, 85-100
        
        completed.forEach(m => {
            const s = m.matchScore;
            if (s < 50) buckets[0]++;
            else if (s < 70) buckets[1]++;
            else if (s < 85) buckets[2]++;
            else buckets[3]++;
        });
        
        // Compute job category volumes
        // Group matches by job Title
        const jobGroups = {};
        completed.forEach(m => {
            const title = m.jobTitle || 'Requisition';
            jobGroups[title] = (jobGroups[title] || 0) + 1;
        });
        
        const jobVolumeData = Object.keys(jobGroups).map(key => ({
            name: key,
            count: jobGroups[key]
        })).sort((a,b) => b.count - a.count);
        
        // Render Charts using Custom components SVG engine
        Components.renderScoreDistributionChart('chart-score-distribution', buckets);
        Components.renderJobVolumeChart('chart-job-category', jobVolumeData);
        
        // Render Summary metrics tables
        summaryContainer.innerHTML = `
            <div class="summary-metric-card">
                <div class="summary-metric-label">Total Candidate Pool</div>
                <div class="summary-metric-value text-info">${stats.totalCandidates || 0}</div>
            </div>
            <div class="summary-metric-card">
                <div class="summary-metric-label">Total Resumes Processed</div>
                <div class="summary-metric-value text-purple">${stats.totalResumes || 0}</div>
            </div>
            <div class="summary-metric-card">
                <div class="summary-metric-label">Average Fit Ratio</div>
                <div class="summary-metric-value text-success">${stats.averageMatchScore ? stats.averageMatchScore + '%' : '0%'}</div>
            </div>
            <div class="summary-metric-card">
                <div class="summary-metric-label">Active Job Positions</div>
                <div class="summary-metric-value text-warning">${stats.totalJobs || 0}</div>
            </div>
        `;
        
    } catch (err) {
        summaryContainer.innerHTML = `<div class="text-center text-danger" style="grid-column:1/-1;">Failed to calculate analytics summary: ${err.message}</div>`;
    }
}

// ---------------------------------------------------------
// TOAST NOTIFICATIONS Helper
// ---------------------------------------------------------
let toastTimeout;
function showToast(title, message, type = 'success') {
    const toast = document.getElementById('toast');
    if (!toast) return;
    
    toast.className = `toast-notification ${type}`;
    
    // Set icon
    const icon = toast.querySelector('.toast-icon');
    if (type === 'success') {
        icon.className = 'toast-icon fa-solid fa-circle-check';
    } else if (type === 'error') {
        icon.className = 'toast-icon fa-solid fa-triangle-exclamation';
    } else {
        icon.className = 'toast-icon fa-solid fa-circle-info';
    }
    
    toast.querySelector('.toast-title').textContent = title;
    toast.querySelector('.toast-message').textContent = message;
    
    toast.classList.remove('hidden');
    
    clearTimeout(toastTimeout);
    toastTimeout = setTimeout(() => {
        toast.classList.add('hidden');
    }, 4500);
}
