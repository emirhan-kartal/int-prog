// SignalR connection setup
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/notificationHub")
    .withAutomaticReconnect()
    .build();

connection.on("ReceiveNotification", function (message) {
    showToast(message);
});

connection.start().catch(function (err) {
    console.error(err.toString());
});

// Toast notification function
function showToast(message, type = 'success') {
    const toast = `
        <div class="toast" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="toast-header">
                <strong class="me-auto">Notification</strong>
                <button type="button" class="btn-close" data-bs-dismiss="toast"></button>
            </div>
            <div class="toast-body">
                ${message}
            </div>
        </div>
    `;
    
    const toastContainer = document.getElementById('toast-container');
    toastContainer.insertAdjacentHTML('beforeend', toast);
    
    const toastElement = toastContainer.lastElementChild;
    const bsToast = new bootstrap.Toast(toastElement);
    bsToast.show();
    
    toastElement.addEventListener('hidden.bs.toast', function () {
        toastElement.remove();
    });
}

// Storage monitoring
function updateStorageUsage() {
    fetch('/api/storage/usage')
        .then(response => response.json())
        .then(data => {
            const progressBar = document.getElementById('storage-progress');
            const storageText = document.getElementById('storage-text');
            
            if (progressBar && storageText) {
                const percentage = (data.used / data.total) * 100;
                progressBar.style.width = `${percentage}%`;
                progressBar.setAttribute('aria-valuenow', percentage);
                progressBar.textContent = `${percentage.toFixed(1)}%`;
                
                const usedGB = (data.used / (1024 * 1024 * 1024)).toFixed(2);
                const totalGB = (data.total / (1024 * 1024 * 1024)).toFixed(2);
                storageText.textContent = `${usedGB} GB used of ${totalGB} GB total`;
                
                // Update progress bar color based on usage
                if (percentage > 90) {
                    progressBar.classList.remove('bg-primary', 'bg-warning');
                    progressBar.classList.add('bg-danger');
                } else if (percentage > 70) {
                    progressBar.classList.remove('bg-primary', 'bg-danger');
                    progressBar.classList.add('bg-warning');
                } else {
                    progressBar.classList.remove('bg-warning', 'bg-danger');
                    progressBar.classList.add('bg-primary');
                }
            }
        })
        .catch(error => console.error('Error fetching storage usage:', error));
}

// Update storage usage every 30 seconds if user is logged in
if (document.getElementById('storage-progress')) {
    updateStorageUsage();
    setInterval(updateStorageUsage, 30000);
}

async function shareFile(fileId) {
    const response = await fetch(`/Files/Share/${fileId}`, {
        method: 'POST',
        headers: {
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
        }
    });
    
    if (response.ok) {
        const data = await response.json();
        // Copy to clipboard
        navigator.clipboard.writeText(data.url);
        showToast('Share link copied to clipboard!');
    }
} 