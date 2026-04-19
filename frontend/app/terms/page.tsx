export default function Terms() {
  return (
    <div className='min-h-screen bg-black text-white p-8'>
      <div className='max-w-4xl mx-auto'>
        <h1 className='text-4xl font-bold mb-8'>Terms of Service</h1>
        <div className='space-y-8 text-gray-300'>
          <p className='text-sm text-gray-400'>Last updated: April 19, 2026</p>

          <section>
            <h2 className='text-2xl font-semibold mb-4 text-white'>1. Acceptance of Terms</h2>
            <p className='mb-3'>
              By accessing and using ServerEye ("the Service"), you accept and agree to be bound by these Terms of Service ("Terms"). If you do not agree to these Terms, please do not use our Service.
            </p>
            <p>
              ServerEye reserves the right to modify these Terms at any time. Your continued use of the Service after such modifications constitutes your acceptance of the updated Terms.
            </p>
          </section>

          <section>
            <h2 className='text-2xl font-semibold mb-4 text-white'>2. Description of Service</h2>
            <p className='mb-3'>
              ServerEye is a server monitoring platform consisting of the following components:
            </p>
            <ul className='list-disc list-inside space-y-2 mb-4'>
              <li><strong className='text-white'>ServerEye Agent:</strong> An open-source Linux monitoring agent (MIT License) installed on your servers as a systemd service</li>
              <li><strong className='text-white'>ServerEye API:</strong> A cloud backend that receives, stores, and serves metrics</li>
              <li><strong className='text-white'>Web Dashboard:</strong> A web-based interface for visualizing metrics and managing servers</li>
              <li><strong className='text-white'>Telegram Bot (@ServereyeTG_bot):</strong> A Telegram bot for remote monitoring and notifications</li>
            </ul>
            <p className='mb-3'>The Agent collects the following metrics at configurable intervals (default 30 seconds):</p>
            <ul className='list-disc list-inside space-y-2 mb-4'>
              <li>CPU usage (total, user, system, idle), load averages, frequency, temperature</li>
              <li>Memory utilization (total, used, available, free, buffers, cached, swap)</li>
              <li>Disk space, I/O statistics, mount points, filesystem types</li>
              <li>Network interface statistics (bytes sent/received, speed, status)</li>
              <li>System uptime, process counts (total/running/sleeping)</li>
              <li>Temperature sensors (CPU, GPU, storage devices)</li>
              <li>Static hardware information sent once per 24 hours (CPU model, RAM modules, motherboard, GPU, network interfaces with MAC addresses)</li>
            </ul>
            <p>
              The Service is provided "as is" and may include bugs, errors, or interruptions.
            </p>
          </section>

          <section>
            <h2 className='text-2xl font-semibold mb-4 text-white'>3. Agent Installation and System Access</h2>
            <p className='mb-3'>
              By installing the ServerEye Agent on your server, you acknowledge and agree that:
            </p>
            <ul className='list-disc list-inside space-y-2 mb-4'>
              <li>The Agent typically runs with elevated (root) privileges as a systemd service in order to read system metrics from <code className='text-blue-300'>/proc</code>, <code className='text-blue-300'>/sys</code>, DMI, and hardware sensors</li>
              <li>The Agent establishes an outbound WebSocket connection (WSS) to the ServerEye API, with HTTP(S) as a fallback transport</li>
              <li>The Agent authenticates using a cryptographically generated secret key (prefix <code className='text-blue-300'>srv_</code>) stored in <code className='text-blue-300'>/etc/servereye/config.yaml</code></li>
              <li>You are the legal owner, administrator, or authorized operator of every server on which you install the Agent</li>
              <li>You are solely responsible for installing, configuring, and uninstalling the Agent</li>
            </ul>
            <p>
              You must not install the Agent on systems you do not own or are not authorized to monitor.
            </p>
          </section>

          <section>
            <h2 className='text-2xl font-semibold mb-4 text-white'>4. User Responsibilities</h2>
            <p className='mb-3'>As a user of ServerEye, you agree to:</p>
            <ul className='list-disc list-inside space-y-2'>
              <li>Provide accurate information when registering servers</li>
              <li>Maintain the security of your account credentials and server secret keys</li>
              <li>Promptly rotate or revoke any secret key that may have been exposed</li>
              <li>Not use the Service for any illegal or unauthorized purpose</li>
              <li>Not attempt to reverse engineer, decompile, or disassemble the cloud-hosted components of the Service (the open-source Agent is governed by its MIT License)</li>
              <li>Not interfere with, overload, or disrupt the Service or other users' servers</li>
              <li>Not use the Service to monitor systems without proper authorization</li>
              <li>Comply with all applicable laws and regulations, including data protection laws in your jurisdiction</li>
            </ul>
          </section>

          <section>
            <h2 className='text-2xl font-semibold mb-4 text-white'>5. Data Collection and Privacy</h2>
            <p className='mb-3'>
              ServerEye collects system metrics from servers on which you install the Agent. By installing the Agent, you consent to the continuous collection and transmission of these metrics to our servers. Heartbeat messages are sent every 60 seconds and metric payloads at the configured collection interval.
            </p>
            <p>
              For detailed information about what is collected, how it is stored, and your data rights, please refer to our Privacy Policy.
            </p>
          </section>

          <section>
            <h2 className='text-2xl font-semibold mb-4 text-white'>6. Account Security</h2>
            <p className='mb-3'>
              You are responsible for maintaining the confidentiality of your account credentials, including your secret keys. You agree to notify us immediately of any unauthorized use of your account or any other breach of security.
            </p>
            <p>
              ServerEye cannot and will not be liable for any loss or damage arising from your failure to comply with this security obligation.
            </p>
          </section>

          <section>
            <h2 className='text-2xl font-semibold mb-4 text-white'>7. Service Availability</h2>
            <p className='mb-3'>
              ServerEye strives to maintain high availability of the Service but does not guarantee uninterrupted access. The Service may be temporarily unavailable for maintenance, updates, or other reasons.
            </p>
            <p>
              We are not liable for any downtime or service interruptions.
            </p>
          </section>

          <section>
            <h2 className='text-2xl font-semibold mb-4 text-white'>8. Limitation of Liability</h2>
            <p className='mb-3'>
              To the fullest extent permitted by applicable law, ServerEye shall not be liable for:
            </p>
            <ul className='list-disc list-inside space-y-2 mb-3'>
              <li>Any indirect, incidental, special, consequential, or punitive damages</li>
              <li>Loss of data, revenue, profits, or business opportunities</li>
              <li>Damages resulting from the use or inability to use the Service</li>
              <li>Damages from unauthorized access to your account or data</li>
            </ul>
            <p>
              In no event shall ServerEye's total liability exceed the amount you paid for the Service in the twelve (12) months preceding the claim.
            </p>
          </section>

          <section>
            <h2 className='text-2xl font-semibold mb-4 text-white'>9. Disclaimer of Warranties</h2>
            <p className='mb-3'>
              The Service is provided "AS IS" and "AS AVAILABLE" without warranties of any kind, either express or implied. ServerEye disclaims all warranties, including but not limited to:
            </p>
            <ul className='list-disc list-inside space-y-2'>
              <li>Merchantability and fitness for a particular purpose</li>
              <li>Non-infringement of third-party rights</li>
              <li>Accuracy, reliability, or availability of the Service</li>
              <li>Security of data transmission or storage</li>
            </ul>
          </section>

          <section>
            <h2 className='text-2xl font-semibold mb-4 text-white'>10. Termination</h2>
            <p className='mb-3'>
              ServerEye reserves the right to suspend or terminate your access to the Service at any time, with or without cause, with or without notice.
            </p>
            <p className='mb-3'>
              You may terminate your account at any time by contacting us or using the account deletion feature in the Service.
            </p>
            <p>
              Upon termination, your right to use the Service will immediately cease. All provisions of these Terms shall survive termination.
            </p>
          </section>

          <section>
            <h2 className='text-2xl font-semibold mb-4 text-white'>11. Intellectual Property</h2>
            <p className='mb-3'>
              All content, features, and functionality of the Service, including but not limited to text, graphics, logos, and software, are the exclusive property of ServerEye and are protected by international copyright, trademark, and other intellectual property laws.
            </p>
            <p>
              You may not reproduce, modify, distribute, or create derivative works based on any part of the Service without our prior written consent.
            </p>
          </section>

          <section>
            <h2 className='text-2xl font-semibold mb-4 text-white'>12. Governing Law</h2>
            <p>
              These Terms shall be governed by and construed in accordance with the laws of the jurisdiction in which ServerEye is established, without regard to its conflict of law provisions.
            </p>
          </section>

          <section>
            <h2 className='text-2xl font-semibold mb-4 text-white'>13. Changes to Terms</h2>
            <p>
              ServerEye reserves the right to modify these Terms at any time. We will notify users of significant changes by posting the new Terms on our website and updating the "Last updated" date. Your continued use of the Service after such changes constitutes your acceptance of the updated Terms.
            </p>
          </section>

          <section>
            <h2 className='text-2xl font-semibold mb-4 text-white'>14. Contact Information</h2>
            <p className='mb-3'>
              If you have any questions about these Terms of Service, please contact us at:
            </p>
            <p className='text-gray-400'>
              Email: support@servereye.dev<br />
              Website: https://servereye.dev
            </p>
          </section>
        </div>
      </div>
    </div>
  );
}
