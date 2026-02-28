export default function Terms() {
  return (
    <div className='min-h-screen bg-black text-white p-8'>
      <div className='max-w-4xl mx-auto'>
        <h1 className='text-4xl font-bold mb-8'>Terms of Service</h1>
        <div className='space-y-6 text-gray-300'>
          <p>Last updated: {new Date().toLocaleDateString()}</p>

          <section>
            <h2 className='text-2xl font-semibold mb-4'>Acceptance of Terms</h2>
            <p>
              By accessing and using ServerEye, you accept and agree to be bound by the terms and
              provision of this agreement.
            </p>
          </section>

          <section>
            <h2 className='text-2xl font-semibold mb-4'>Use License</h2>
            <p>
              Permission is granted to temporarily use ServerEye for personal, non-commercial
              transitory viewing only.
            </p>
          </section>

          <section>
            <h2 className='text-2xl font-semibold mb-4'>Disclaimer</h2>
            <p>
              The information on this website is provided on an as-is basis. To the fullest extent
              permitted by law, this Company:
            </p>
            <ul className='list-disc list-inside mt-2'>
              <li>
                excludes all representations and warranties relating to this website and its
                contents
              </li>
              <li>
                excludes all liability for damages arising out of or in connection with your use of
                this website
              </li>
            </ul>
          </section>

          <section>
            <h2 className='text-2xl font-semibold mb-4'>Contact Information</h2>
            <p>Questions about the Terms of Service should be sent to us at.</p>
          </section>
        </div>
      </div>
    </div>
  );
}
