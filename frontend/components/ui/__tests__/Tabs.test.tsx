import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import '@testing-library/jest-dom';
import { TabsNavigation, TabsContent, TabPanel } from '../Tabs';
import { Home, Settings } from 'lucide-react';

const sampleTabs = [
  { id: 'home', label: 'Home', icon: Home },
  { id: 'settings', label: 'Settings', icon: Settings },
  { id: 'disabled-tab', label: 'Disabled', icon: Settings, disabled: true },
];

describe('TabsNavigation', () => {
  it('should render all tabs', () => {
    const onTabChange = jest.fn();
    render(<TabsNavigation tabs={sampleTabs} activeTab='home' onTabChange={onTabChange} />);
    expect(screen.getByText('Home')).toBeInTheDocument();
    expect(screen.getByText('Settings')).toBeInTheDocument();
    expect(screen.getByText('Disabled')).toBeInTheDocument();
  });

  it('should apply active styles to the active tab', () => {
    const onTabChange = jest.fn();
    render(<TabsNavigation tabs={sampleTabs} activeTab='home' onTabChange={onTabChange} />);
    const homeButton = screen.getByText('Home').closest('button');
    expect(homeButton).toHaveClass('text-blue-400');
  });

  it('should not apply active styles to inactive tabs', () => {
    const onTabChange = jest.fn();
    render(<TabsNavigation tabs={sampleTabs} activeTab='home' onTabChange={onTabChange} />);
    const settingsButton = screen.getByText('Settings').closest('button');
    expect(settingsButton).not.toHaveClass('text-blue-400');
  });

  it('should call onTabChange when an enabled tab is clicked', () => {
    const onTabChange = jest.fn();
    render(<TabsNavigation tabs={sampleTabs} activeTab='home' onTabChange={onTabChange} />);
    fireEvent.click(screen.getByText('Settings'));
    expect(onTabChange).toHaveBeenCalledWith('settings');
  });

  it('should not call onTabChange when a disabled tab is clicked', () => {
    const onTabChange = jest.fn();
    render(<TabsNavigation tabs={sampleTabs} activeTab='home' onTabChange={onTabChange} />);
    fireEvent.click(screen.getByText('Disabled'));
    expect(onTabChange).not.toHaveBeenCalled();
  });

  it('should disable the disabled tab button', () => {
    const onTabChange = jest.fn();
    render(<TabsNavigation tabs={sampleTabs} activeTab='home' onTabChange={onTabChange} />);
    const disabledButton = screen.getByText('Disabled').closest('button');
    expect(disabledButton).toBeDisabled();
  });

  it('should render with no tabs without crashing', () => {
    const onTabChange = jest.fn();
    render(<TabsNavigation tabs={[]} activeTab='' onTabChange={onTabChange} />);
    expect(screen.queryAllByRole('button')).toHaveLength(0);
  });
});

describe('TabsContent', () => {
  it('should render children', () => {
    render(<TabsContent activeTab='home'>Tab body</TabsContent>);
    expect(screen.getByText('Tab body')).toBeInTheDocument();
  });

  it('should always render regardless of activeTab value', () => {
    render(<TabsContent activeTab='some-tab'>Always visible wrapper</TabsContent>);
    expect(screen.getByText('Always visible wrapper')).toBeInTheDocument();
  });
});

describe('TabPanel', () => {
  it('should render children when value matches activeTab', () => {
    render(
      <TabPanel value='home' activeTab='home'>
        Home panel content
      </TabPanel>,
    );
    expect(screen.getByText('Home panel content')).toBeInTheDocument();
  });

  it('should not render children when value does not match activeTab', () => {
    render(
      <TabPanel value='settings' activeTab='home'>
        Settings panel content
      </TabPanel>,
    );
    expect(screen.queryByText('Settings panel content')).not.toBeInTheDocument();
  });

  it('should show correct panel when activeTab changes', () => {
    const { rerender } = render(
      <TabPanel value='settings' activeTab='home'>
        Settings content
      </TabPanel>,
    );
    expect(screen.queryByText('Settings content')).not.toBeInTheDocument();

    rerender(
      <TabPanel value='settings' activeTab='settings'>
        Settings content
      </TabPanel>,
    );
    expect(screen.getByText('Settings content')).toBeInTheDocument();
  });
});
