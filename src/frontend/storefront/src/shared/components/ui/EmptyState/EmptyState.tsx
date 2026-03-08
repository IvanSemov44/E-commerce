import React from 'react';
import Button from 'path/to/button';  // Ensure Button is imported

const ErrorState = () => {
    return (
        <div>
            <h1>Error Occurred</h1>
            <Button onClick={() => alert('Retry')}>Retry</Button>
        </div>
    );
};

export default ErrorState;