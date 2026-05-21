/**
 * TalentAI Component Library
 * Custom SVG-based chart engines and visual UI widgets.
 */

const Components = {
    /**
     * Renders a glowing radial gauge for candidate match score
     * @param {string} elementId - Target DOM element
     * @param {number} percentage - Match score value (0 to 100)
     */
    renderRadialGauge(elementId, percentage) {
        const valueEl = document.getElementById(elementId);
        if (!valueEl) return;

        const rounded = Math.round(percentage);
        valueEl.textContent = `${rounded}%`;
        
        const ring = valueEl.closest('.gauge-ring');
        if (ring) {
            // Determine gauge color based on score
            let color = 'var(--accent-danger)';
            let glow = 'var(--accent-danger-glow)';
            if (rounded >= 80) {
                color = 'var(--accent-success)';
                glow = 'var(--accent-success-glow)';
            } else if (rounded >= 55) {
                color = 'var(--accent-warning)';
                glow = 'var(--accent-warning-glow)';
            }
            
            ring.style.setProperty('--gauge-percent', `${rounded}%`);
            ring.style.background = `radial-gradient(circle, var(--bg-secondary) 55%, transparent 56%), conic-gradient(${color} ${rounded}%, rgba(255, 255, 255, 0.05) ${rounded}%)`;
            ring.style.boxShadow = `inset 0 0 10px rgba(0, 0, 0, 0.5), 0 4px 25px ${glow}`;
        }
    },

    /**
     * Renders a beautiful SVG bar chart representing score distribution.
     * @param {string} containerId - Target container ID
     * @param {Array<number>} data - Array of counts for each score bucket [0-50, 50-70, 70-85, 85-100]
     */
    renderScoreDistributionChart(containerId, data = [0, 0, 0, 0]) {
        const container = document.getElementById(containerId);
        if (!container) return;

        const maxVal = Math.max(...data, 1);
        const padding = 40;
        const width = container.clientWidth || 450;
        const height = container.clientHeight || 260;
        const chartWidth = width - padding * 2;
        const chartHeight = height - padding * 2;
        
        const labels = ['0-50 (Weak)', '50-70 (Fair)', '70-85 (Good)', '85-100 (Strong)'];
        const colors = ['#ef4444', '#f59e0b', '#06b6d4', '#10b981'];
        const barWidth = (chartWidth / data.length) - 20;

        let svgHtml = `
            <svg class="chart-svg" width="100%" height="100%" viewBox="0 0 ${width} ${height}">
                <defs>
                    <linearGradient id="gridGrad" x1="0" y1="0" x2="0" y2="1">
                        <stop offset="0%" stop-color="rgba(255,255,255,0.01)"/>
                        <stop offset="100%" stop-color="rgba(255,255,255,0.05)"/>
                    </linearGradient>
                    ${data.map((_, i) => `
                        <linearGradient id="barGrad-${i}" x1="0" y1="0" x2="0" y2="1">
                            <stop offset="0%" stop-color="${colors[i]}" stop-opacity="0.85"/>
                            <stop offset="100%" stop-color="${colors[i]}" stop-opacity="0.25"/>
                        </linearGradient>
                    `).join('')}
                </defs>

                <!-- Gridlines -->
                <line x1="${padding}" y1="${padding}" x2="${width - padding}" y2="${padding}" stroke="rgba(255,255,255,0.05)" stroke-dasharray="4"/>
                <line x1="${padding}" y1="${padding + chartHeight/2}" x2="${width - padding}" y2="${padding + chartHeight/2}" stroke="rgba(255,255,255,0.05)" stroke-dasharray="4"/>
                <line x1="${padding}" y1="${height - padding}" x2="${width - padding}" y2="${height - padding}" stroke="rgba(255,255,255,0.15)"/>

                <!-- Y Axis Labels -->
                <text x="${padding - 10}" y="${padding + 5}" class="chart-axis-label" text-anchor="end">${maxVal}</text>
                <text x="${padding - 10}" y="${padding + chartHeight/2 + 5}" class="chart-axis-label" text-anchor="end">${Math.round(maxVal/2)}</text>
                <text x="${padding - 10}" y="${height - padding + 5}" class="chart-axis-label" text-anchor="end">0</text>
        `;

        data.forEach((val, i) => {
            const barHeight = (val / maxVal) * chartHeight;
            const x = padding + (i * (chartWidth / data.length)) + 10;
            const y = height - padding - barHeight;

            svgHtml += `
                <!-- Bar -->
                <rect x="${x}" y="${y}" width="${barWidth}" height="${barHeight}" rx="4" fill="url(#barGrad-${i})" stroke="${colors[i]}" stroke-width="1" class="chart-bar">
                    <animate attributeName="height" from="0" to="${barHeight}" dur="0.6s" fill="freeze" />
                    <animate attributeName="y" from="${height - padding}" to="${y}" dur="0.6s" fill="freeze" />
                </rect>
                <!-- Tooltip / Label Count on Bar -->
                <text x="${x + barWidth/2}" y="${y - 8}" fill="${colors[i]}" font-size="12" font-weight="600" text-anchor="middle" font-family="var(--font-body)">
                    ${val}
                    <animate attributeName="opacity" from="0" to="1" dur="0.8s" fill="freeze"/>
                </text>
                <!-- X Axis Labels -->
                <text x="${x + barWidth/2}" y="${height - padding + 20}" class="chart-axis-label" text-anchor="middle">${labels[i]}</text>
            `;
        });

        svgHtml += `</svg>`;
        container.innerHTML = svgHtml;
    },

    /**
     * Renders a sleek horizontal SVG bar chart for requisitions breakdown.
     * @param {string} containerId - Target container ID
     * @param {Array<object>} items - List of { name: 'Job Title', count: number }
     */
    renderJobVolumeChart(containerId, items = []) {
        const container = document.getElementById(containerId);
        if (!container) return;

        if (items.length === 0) {
            container.innerHTML = `<div class="timeline-empty">No active requisition metrics</div>`;
            return;
        }

        const maxVal = Math.max(...items.map(x => x.count), 1);
        const paddingLeft = 140;
        const paddingRight = 40;
        const paddingTop = 20;
        const paddingBottom = 20;
        
        const width = container.clientWidth || 450;
        const height = container.clientHeight || 260;
        const chartWidth = width - paddingLeft - paddingRight;
        const chartHeight = height - paddingTop - paddingBottom;
        
        const barHeight = Math.min(30, (chartHeight / items.length) - 10);
        
        let svgHtml = `
            <svg class="chart-svg" width="100%" height="100%" viewBox="0 0 ${width} ${height}">
                <defs>
                    <linearGradient id="horizBarGrad" x1="0" y1="0" x2="1" y2="0">
                        <stop offset="0%" stop-color="var(--accent-primary)" stop-opacity="0.3"/>
                        <stop offset="100%" stop-color="var(--accent-purple)" stop-opacity="0.9"/>
                    </linearGradient>
                </defs>
        `;

        items.forEach((item, i) => {
            const barWidth = (item.count / maxVal) * chartWidth;
            const y = paddingTop + (i * (chartHeight / items.length)) + 5;
            const x = paddingLeft;

            // Crop job title if too long
            const maxChars = 20;
            const displayName = item.name.length > maxChars ? item.name.substring(0, maxChars) + '...' : item.name;

            svgHtml += `
                <!-- Name Label -->
                <text x="${paddingLeft - 12}" y="${y + barHeight/2 + 4}" fill="var(--text-secondary)" font-size="11" text-anchor="end" font-family="var(--font-body)">
                    ${displayName}
                </text>
                <!-- Bar Background Track -->
                <rect x="${x}" y="${y}" width="${chartWidth}" height="${barHeight}" rx="4" fill="rgba(255, 255, 255, 0.02)" stroke="rgba(255,255,255,0.03)" stroke-width="1" />
                <!-- Active Bar -->
                <rect x="${x}" y="${y}" width="${barWidth}" height="${barHeight}" rx="4" fill="url(#horizBarGrad)" stroke="var(--accent-purple)" stroke-width="1">
                    <animate attributeName="width" from="0" to="${barWidth}" dur="0.6s" fill="freeze" />
                </rect>
                <!-- Count text -->
                <text x="${x + barWidth + 10}" y="${y + barHeight/2 + 4}" fill="white" font-weight="600" font-size="11" font-family="var(--font-body)">
                    ${item.count}
                    <animate attributeName="opacity" from="0" to="1" dur="0.8s" fill="freeze" />
                </text>
            `;
        });

        svgHtml += `</svg>`;
        container.innerHTML = svgHtml;
    }
};
