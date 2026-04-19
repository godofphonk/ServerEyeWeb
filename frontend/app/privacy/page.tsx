export default function Privacy() {
  return (
    <div className='min-h-screen bg-black text-white p-8'>
      <div className='max-w-4xl mx-auto'>
        <h1 className='text-4xl font-bold mb-8'>Privacy Policy</h1>
        <div className='space-y-8 text-gray-300'>
          <p className='text-sm text-gray-400'>Last updated: April 19, 2026</p>

          <section>
            <h2 className='text-2xl font-semibold mb-4 text-white'>1. Introduction</h2>
            <p>
              ServerEye ("we", "our", "us") is committed to protecting your privacy. This Privacy Policy explains how we collect, use, store, and protect your information when you use our server monitoring service.
            </p>
          </section>

          <section>
            <h2 className='text-2xl font-semibold mb-4 text-white'>2. Information We Collect</h2>
            
            <h3 className='text-xl font-semibold mb-3 mt-6 text-white'>2.1 System Metrics</h3>
            <p className='mb-3'>
              When you install the ServerEye agent on your servers, we collect the following system metrics:
            </p>
            <ul className='list-disc list-inside space-y-2 mb-4'>
              <li><strong className='text-white'>CPU Metrics:</strong> Usage percentages, load averages, frequency, temperature</li>
              <li><strong className='text-white'>Memory Metrics:</strong> Total, used, available, cached, swap memory</li>
              <li><strong className='text-white'>Disk Metrics:</strong> Usage, I/O statistics, mount points, filesystem types</li>
              <li><strong className='text-white'>Network Metrics:</strong> Interface statistics, traffic volume, connection counts</li>
              <li><strong className='text-white'>System Information:</strong> Hostname, OS version, kernel version, architecture, uptime</li>
              <li><strong className='text-white'>Process Information:</strong> Running processes, CPU/memory usage per process</li>
            </ul>

            <h3 className='text-xl font-semibold mb-3 mt-6 text-white'>2.2 Account Information</h3>
            <p className='mb-3'>
              When you create an account, we collect:
            </p>
            <ul className='list-disc list-inside space-y-2 mb-4'>
              <li>Email address</li>
              <li>Username/display name</li>
              <li>Authentication credentials (hashed and salted)</li>
              <li>Secret keys for server authentication</li>
            </ul>

            <h3 className='text-xl font-semibold mb-3 mt-6 text-white'>2.3 Usage Data</h3>
            <p className='mb-3'>
              We automatically collect information about your use of the Service:
            </p>
            <ul className='list-disc list-inside space-y-2'>
              <li>Access logs and timestamps</li>
              <li>IP address and approximate location</li>
              <li>Browser type and version</li>
              <li>Device information</li>
              <li>Pages visited and features used</li>
            </ul>
          </section>

          <section>
            <h2 className='text-2xl font-semibold mb-4 text-white'>3. How We Use Your Information</h2>
            <p className='mb-3'>We use the collected information for the following purposes:</p>
            <ul className='list-disc list-inside space-y-2'>
              <li><strong className='text-white'>Service Provision:</strong> To provide monitoring, alerting, and visualization features</li>
              <li><strong className='text-white'>Service Improvement:</strong> To analyze usage patterns and improve our services</li>
              <li><strong className='text-white'>Security:</strong> To detect and prevent fraudulent activities and abuse</li>
              <li><strong className='text-white'>Communication:</strong> To send you important notices about your account</li>
              <li><strong className='text-white'>Support:</strong> To provide customer support and troubleshoot issues</li>
              <li><strong className='text-white'>Analytics:</strong> To generate aggregated, anonymized statistics about service usage</li>
            </ul>
          </section>

          <section>
            <h2 className='text-2xl font-semibold mb-4 text-white'>4. Data Storage and Retention</h2>
            <p className='mb-3'>
              Your data is stored securely in our databases:
            </p>
            <ul className='list-disc list-inside space-y-2 mb-4'>
              <li><strong className='text-white'>Time-series Metrics:</strong> Stored in TimescaleDB with configurable retention periods (default: 30 days for detailed metrics, 1 year for aggregated data)</li>
              <li><strong className='text-white'>Account Data:</strong> Stored in PostgreSQL and retained until account deletion</li>
              <li><strong className='text-white'>Logs:</strong> Access and error logs retained for 90 days for security and debugging purposes</li>
            </ul>
            <p>
              You can configure data retention periods in your account settings. Historical data beyond retention periods is automatically deleted.
            </p>
          </section>

          <section>
            <h2 className='text-2xl font-semibold mb-4 text-white'>5. Data Security</h2>
            <p className='mb-3'>We implement reasonable security measures to protect your information:</p>
            <ul className='list-disc list-inside space-y-2 mb-4'>
              <li><strong className='text-white'>Encryption:</strong> All data in transit is encrypted using TLS 1.3</li>
              <li><strong className='text-white'>Encryption at Rest:</strong> Database encryption for sensitive data</li>
              <li><strong className='text-white'>Access Control:</strong> Strict access controls and authentication for our staff</li>
              <li><strong className='text-white'>Secret Keys:</strong> Server authentication using cryptographically secure secret keys</li>
              <li><strong className='text-white'>Regular Audits:</strong> Periodic security audits and penetration testing</li>
            </ul>
            <p>
              However, no method of transmission over the Internet is 100% secure. While we strive to protect your data, we cannot guarantee absolute security.
            </p>
          </section>

          <section>
            <h2 className='text-2xl font-semibold mb-4 text-white'>6. Data Sharing and Disclosure</h2>
            <p className='mb-3'>We do not sell your data. We may share your information only in the following circumstances:</p>
            <ul className='list-disc list-inside space-y-2'>
              <li><strong className='text-white'>Service Providers:</strong> With trusted third-party service providers who assist in operating our service (e.g., cloud hosting, analytics)</li>
              <li><strong className='text-white'>Legal Requirements:</strong> When required by law, court order, or government request</li>
              <li><strong className='text-white'>Business Transfer:</strong> In connection with a merger, acquisition, or sale of assets</li>
              <li><strong className='text-white'>Protection:</strong> To protect our rights, property, or safety, or that of our users</li>
            </ul>
          </section>

          <section>
            <h2 className='text-2xl font-semibold mb-4 text-white'>7. Your Rights and Choices</h2>
            <p className='mb-3'>You have the following rights regarding your data:</p>
            <ul className='list-disc list-inside space-y-2 mb-4'>
              <li><strong className='text-white'>Access:</strong> Request a copy of your personal data</li>
              <li><strong className='text-white'>Correction:</strong> Update or correct inaccurate information</li>
              <li><strong className='text-white'>Deletion:</strong> Request deletion of your account and associated data</li>
              <li><strong className='text-white'>Export:</strong> Export your data in a machine-readable format</li>
              <li><strong className='text-white'>Opt-out:</strong> Disable monitoring for specific servers at any time</li>
              <li><strong className='text-white'>Retention:</strong> Configure data retention periods for your metrics</li>
            </ul>
            <p>
              To exercise these rights, please contact us at support@servereye.dev. We will respond within 30 days.
            </p>
          </section>

          <section>
            <h2 className='text-2xl font-semibold mb-4 text-white'>8. Cookies and Tracking</h2>
            <p className='mb-3'>
              We use cookies and similar technologies to:
            </p>
            <ul className='list-disc list-inside space-y-2 mb-4'>
              <li>Maintain your authentication session</li>
              <li>Remember your preferences and settings</li>
              <li>Analyze website usage and improve performance</li>
              <li>Provide personalized content and features</li>
            </ul>
            <p>
              You can control cookies through your browser settings. Note that disabling cookies may affect the functionality of the Service.
            </p>
          </section>

          <section>
            <h2 className='text-2xl font-semibold mb-4 text-white'>9. Third-Party Services</h2>
            <p className='mb-3'>
              Our Service may integrate with third-party services:
            </p>
            <ul className='list-disc list-inside space-y-2 mb-4'>
              <li><strong className='text-white'>Telegram:</strong> For bot-based monitoring notifications</li>
              <li><strong className='text-white'>Cloud Providers:</strong> For hosting our infrastructure</li>
              <li><strong className='text-white'>Analytics:</strong> For usage analytics and error tracking</li>
            </ul>
            <p>
              These third parties have their own privacy policies. We encourage you to review them.
            </p>
          </section>

          <section>
            <h2 className='text-2xl font-semibold mb-4 text-white'>10. Children's Privacy</h2>
            <p>
              Our Service is not intended for children under the age of 13. We do not knowingly collect personal information from children under 13. If you are a parent or guardian and believe your child has provided us with personal information, please contact us.
            </p>
          </section>

          <section>
            <h2 className='text-2xl font-semibold mb-4 text-white'>11. International Data Transfers</h2>
            <p>
              Your information may be transferred to and processed in countries other than your country of residence. We ensure appropriate safeguards are in place to protect your data in accordance with this Privacy Policy and applicable data protection laws.
            </p>
          </section>

          <section>
            <h2 className='text-2xl font-semibold mb-4 text-white'>12. Changes to This Privacy Policy</h2>
            <p>
              We may update this Privacy Policy from time to time. We will notify you of significant changes by posting the new policy on our website and updating the "Last updated" date. Your continued use of the Service after such changes constitutes your acceptance of the updated policy.
            </p>
          </section>

          <section>
            <h2 className='text-2xl font-semibold mb-4 text-white'>13. Contact Us</h2>
            <p className='mb-3'>
              If you have any questions about this Privacy Policy or our data practices, please contact us:
            </p>
            <p className='text-gray-400 mb-4'>
              Email: support@servereye.dev<br />
              Website: https://servereye.dev
            </p>
            <p>
              For data protection inquiries, please include "Privacy Request" in your email subject line.
            </p>
          </section>
        </div>
      </div>
    </div>
  );
}
