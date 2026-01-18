/**
 * Simple fluid background animation using Canvas
 * Simulates "ColorBends" / "Mesh Gradient" style.
 */

document.addEventListener('DOMContentLoaded', () => {
    // initFluidBackground(); // Disabled to revert to original design
    initThemeToggle();
});

function initThemeToggle() {
    const toggleBtn = document.getElementById('theme-toggle');
    if (!toggleBtn) return;

    const html = document.documentElement;
    const currentTheme = localStorage.getItem('theme') || 'light';
    html.setAttribute('data-theme', currentTheme);
    updateToggleIcon(toggleBtn, currentTheme);

    toggleBtn.addEventListener('click', () => {
        const theme = html.getAttribute('data-theme');
        const newTheme = theme === 'light' ? 'dark' : 'light';

        html.setAttribute('data-theme', newTheme);
        localStorage.setItem('theme', newTheme);
        updateToggleIcon(toggleBtn, newTheme);
    });
}

function updateToggleIcon(btn, theme) {
    if (theme === 'dark') {
        btn.innerHTML = '<i class="bi bi-sun-fill"></i>';
    } else {
        btn.innerHTML = '<i class="bi bi-moon-stars-fill"></i>';
    }
}

function initFluidBackground() {
    const canvas = document.getElementById('fluid-canvas');
    if (!canvas) return;

    const ctx = canvas.getContext('2d');

    // Resize
    let width, height;
    function resize() {
        width = canvas.width = window.innerWidth;
        height = canvas.height = window.innerHeight;
    }
    window.addEventListener('resize', resize);
    resize();

    // Blobs
    // Colors: We use RGBA for blending
    // Yellow: 255, 193, 7
    // Brown: 62, 39, 35
    // Black/Dark: 18, 18, 18
    // Cream: 255, 253, 231 (only in light mode mostly, but we can mix it)

    class Blob {
        constructor() {
            this.init();
        }

        init() {
            this.x = Math.random() * width;
            this.y = Math.random() * height;
            this.vx = (Math.random() - 0.5) * 1.5; // slow movement
            this.vy = (Math.random() - 0.5) * 1.5;
            this.radius = Math.random() * 300 + 200; // Big blobs

            // Random color selection from our palette
            const colors = [
                { r: 255, g: 193, b: 7 },   // Yellow
                { r: 62, g: 39, b: 35 },    // Brown
                { r: 30, g: 30, b: 30 },    // Dark Grey
                { r: 200, g: 160, b: 60 }   // Gold/Dark Yellow mix
            ];
            this.color = colors[Math.floor(Math.random() * colors.length)];
            this.alpha = Math.random() * 0.5 + 0.2;
            this.angle = Math.random() * Math.PI * 2;
        }

        update() {
            this.x += this.vx;
            this.y += this.vy;

            // Bounce off edges (softly)
            if (this.x < -this.radius) this.x = width + this.radius;
            if (this.x > width + this.radius) this.x = -this.radius;
            if (this.y < -this.radius) this.y = height + this.radius;
            if (this.y > height + this.radius) this.y = -this.radius;
        }

        draw(ctx) {
            ctx.beginPath();
            // Gradient for 3D/Soft feel
            const gradient = ctx.createRadialGradient(this.x, this.y, 0, this.x, this.y, this.radius);
            gradient.addColorStop(0, `rgba(${this.color.r}, ${this.color.g}, ${this.color.b}, ${this.alpha})`);
            gradient.addColorStop(1, `rgba(${this.color.r}, ${this.color.g}, ${this.color.b}, 0)`);

            ctx.fillStyle = gradient;
            ctx.arc(this.x, this.y, this.radius, 0, Math.PI * 2);
            ctx.fill();
        }
    }

    const blobs = [];
    for (let i = 0; i < 6; i++) {
        blobs.push(new Blob());
    }

    // Animation Loop
    function animate() {
        // Clear with a base color depending on theme (or transparency to let body bg show)
        // We want 'dark theme weighted' so base is dark brown/black
        ctx.fillStyle = '#121212';
        ctx.fillRect(0, 0, width, height);

        // Draw blobs
        blobs.forEach(blob => {
            blob.update();
            blob.draw(ctx);
        });

        requestAnimationFrame(animate);
    }

    animate();
}
