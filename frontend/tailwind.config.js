theme: {
    extend: {
        animation: { 'fade-in-right': 'fadeInRight 0.3s ease-out forwards' },
        keyframes: {
            fadeInRight: {
                '0%': { opacity: '0', transform: 'translateX(100%)' },
                '100%': { opacity: '1', transform: 'translateX(0)' },
            }
        }
    }
}